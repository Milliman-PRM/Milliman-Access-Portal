using AuditLogLib.Services;
using ContainerizedAppLib;
using ContainerizedAppLib.AzureRestApiModels;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.ContentProxy;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace MillimanAccessPortal.Services
{
    public class ContainerInstanceMonitorHostedService : BackgroundService
    {
        private readonly TimeSpan _previewContainerLingerTime = TimeSpan.FromMinutes(30);

        // singleton services
        private readonly IServiceProvider _services;
        private readonly IConfiguration _appConfig;
        private readonly ContainerizedAppLibApiConfig _containerizedAppLibApiConfig;
        private readonly MapProxyConfigProvider _proxyConfigProvider;

        // scoped services
        private IAuditLogger _auditLogger;
        private ApplicationDbContext _dbContext;
        private ContainerizedAppLibApi _containerizedAppLibApi;

        public ContainerInstanceMonitorHostedService(IServiceProvider servicesArg, 
                                                     IConfiguration appConfigArg, 
                                                     IOptions<ContainerizedAppLibApiConfig> containerizedAppLibApiConfigArg,
                                                     IProxyConfigProvider proxyConfigProvider)
        {
            _services = servicesArg;
            _appConfig = appConfigArg;
            _containerizedAppLibApiConfig = containerizedAppLibApiConfigArg.Value;
            _proxyConfigProvider = proxyConfigProvider as MapProxyConfigProvider;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        List<Task> AllParallelTasks = new List<Task>();

                        _auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                        _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        _containerizedAppLibApi = await new ContainerizedAppLibApi(_containerizedAppLibApiConfig).InitializeAsync(null);

                        List<ContainerGroup_GetResponseModel> allContainerGroups = await _containerizedAppLibApi.ListContainerGroupsInResourceGroup();
                        List<ContainerGroup_GetResponseModel> previewContainerGroups = allContainerGroups.Where(cg => cg.Tags.ContainsKey("publicationRequestId")).ToList();
                        List<ContainerGroup_GetResponseModel> liveContainerGroups = allContainerGroups.Where(cg => cg.Tags.ContainsKey("selectionGroupId")).ToList();

                        Log.Information($"Last activity collection is{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", GlobalFunctions.ContainerLastActivity.Select(a => a.Key + ": " + a.Value.ToString()))}");
                        Log.Information($"Found {allContainerGroups.Count} container groups, {previewContainerGroups.Count} preview, {liveContainerGroups.Count} live");

                        List<ContentPublicationRequest> pendingPublications = await _dbContext.ContentPublicationRequest
                                                                                              .Include(p => p.RootContentItem)
                                                                                                  .ThenInclude(rc => rc.Client)
                                                                                                      .ThenInclude(c => c.ProfitCenter)
                                                                                              .Where(p => p.RootContentItem.ContentType.TypeEnum == ContentTypeEnum.ContainerApp)
                                                                                              .Where(p => p.RequestStatus == PublicationStatus.Processed)
                                                                                              // .Where(p => p.CreateDateTimeUtc > DateTime.UtcNow - TimeSpan.FromDays(7))   TODO is this a good idea?
                                                                                              .ToListAsync();

                        Log.Debug($"Preview publications: {Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", pendingPublications.Select(p => $"initiated:{p.CreateDateTimeUtc}, content:{p.RootContentItem.ContentName}"))}");

                        #region Manage preview container instances
                        TimeSpan previewLingerTime = TimeSpan.FromMinutes(30);
                        foreach (ContentPublicationRequest publication in pendingPublications)
                        {
                            ContainerGroup_GetResponseModel runningContainer = previewContainerGroups.SingleOrDefault(c => c.Name == publication.Id.ToString());

                            string contentToken = GlobalFunctions.HexMd5String(publication.Id);

                            DateTime lastActivity = GlobalFunctions.ContainerLastActivity.ContainsKey(contentToken)
                                ? GlobalFunctions.ContainerLastActivity[contentToken]
                                : DateTime.MinValue;

                            // 1) If a preview container should be running and it is not then launch one
                            if ((DateTime.UtcNow < publication.OutcomeMetadataObj.StartDateTime + TimeSpan.FromMinutes(30) ||
                                 DateTime.UtcNow < lastActivity + previewLingerTime) &&
                                runningContainer == null
                               )
                            {
                                ContainerGroupResourceTags tags = new()
                                {
                                    ProfitCenterId = publication.RootContentItem.Client.ProfitCenterId,
                                    ProfitCenterName = publication.RootContentItem.Client.ProfitCenter.Name,
                                    ClientId = publication.RootContentItem.ClientId,
                                    ClientName = publication.RootContentItem.Client.Name,
                                    ContentItemId = publication.RootContentItem.Id,
                                    ContentItemName = publication.RootContentItem.ContentName,
                                    SelectionGroupId = null,
                                    SelectionGroupName = null,
                                    PublicationRequestId = publication.Id,
                                    ContentToken = contentToken,
                                };

                                AllParallelTasks.Add(RequestContainer(publication.Id, publication.RootContentItem, true, contentToken, tags));
                            }

                            // 2) If a preview container should NOT be running and it is then delete it
                            if (DateTime.UtcNow > publication.OutcomeMetadataObj.StartDateTime + TimeSpan.FromMinutes(30) &&
                                DateTime.UtcNow > lastActivity + previewLingerTime &&
                                runningContainer != null
                               )
                            {
                                AllParallelTasks.Add(TerminateContainerAsync(publication.Id.ToString(), contentToken));
                            }
                        }
                        #endregion

                        List<SelectionGroup> liveSelectionGroups = await _dbContext.SelectionGroup
                                                                                   .Include(p => p.RootContentItem)
                                                                                       .ThenInclude(rc => rc.ContentType)
                                                                                   .Include(p => p.RootContentItem)
                                                                                       .ThenInclude(rc => rc.Client)
                                                                                           .ThenInclude(c => c.ProfitCenter)
                                                                                   .Where(sg => sg.RootContentItem.ContentType.TypeEnum == ContentTypeEnum.ContainerApp)
                                                                                   .Where(sg => _dbContext.ContentPublicationRequest.Any(p => p.RootContentItemId == sg.RootContentItemId
                                                                                                                                           && p.RequestStatus == PublicationStatus.Confirmed))
                                                                                   .ToListAsync();

                        Log.Debug($"Live selection groups: {Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", liveSelectionGroups.Select(g => $"group:{g.GroupName}, content:{g.RootContentItem.ContentName}"))}");

                        #region Manage live selection group container instances
                        foreach (SelectionGroup selectionGroup in liveSelectionGroups)
                        {
                            ContainerizedAppContentItemProperties contentItemProps = selectionGroup.RootContentItem.TypeSpecificDetailObject as ContainerizedAppContentItemProperties;
                            bool containerIsScheduledToRunNow = false;
                            TimeSpan lingerTime = contentItemProps.LiveContainerLifetimeScheme.ContainerLingerTimeAfterActivity;

                            if (contentItemProps.LiveContainerLifetimeScheme.Scheme == ContainerInstanceLifetimeSchemeEnum.Custom) {
                                ContainerizedAppContentItemProperties.CustomScheduleLifetimeScheme customScheme = (ContainerizedAppContentItemProperties.CustomScheduleLifetimeScheme)contentItemProps.LiveContainerLifetimeScheme;
                                containerIsScheduledToRunNow = customScheme.IsScheduledOnNow();
                            }

                            ContainerGroup_GetResponseModel runningContainer = liveContainerGroups.SingleOrDefault(c => c.Name == selectionGroup.Id.ToString());
                            string contentToken = GlobalFunctions.HexMd5String(selectionGroup.Id);

                            DateTime lastActivity = GlobalFunctions.ContainerLastActivity.ContainsKey(contentToken)
                                ? GlobalFunctions.ContainerLastActivity[contentToken]
                                : DateTime.MinValue;

                            Log.Debug($"\tFor selection group {selectionGroup.GroupName}: lastActivity={lastActivity}, containerIsScheduledToRunNow={containerIsScheduledToRunNow}, lingerTime={lingerTime}, running container={runningContainer?.Name ?? "<none>"}");

                            // 3) If a live container should be running and it is not then launch one
                            if ((containerIsScheduledToRunNow ||
                                DateTime.UtcNow < lastActivity + lingerTime) &&
                                runningContainer == null
                               )
                            {
                                Log.Debug($"\tPreparing to launch missing container");
                                ContainerGroupResourceTags tags = new()
                                {
                                    ProfitCenterId = selectionGroup.RootContentItem.Client.ProfitCenterId,
                                    ProfitCenterName = selectionGroup.RootContentItem.Client.ProfitCenter.Name,
                                    ClientId = selectionGroup.RootContentItem.ClientId,
                                    ClientName = selectionGroup.RootContentItem.Client.Name,
                                    ContentItemId = selectionGroup.RootContentItem.Id,
                                    ContentItemName = selectionGroup.RootContentItem.ContentName,
                                    SelectionGroupId = selectionGroup.Id,
                                    SelectionGroupName = selectionGroup.GroupName,
                                    PublicationRequestId = null,
                                    ContentToken = contentToken,
                                };

                                AllParallelTasks.Add(RequestContainer(selectionGroup.Id, selectionGroup.RootContentItem, true, contentToken, tags));
                            }
                            else
                            {
                                Log.Debug($"\tA container {(runningContainer == null ? "will not be launched" : "is already running")}");
                            }

                            // 4) If a live container should NOT be running and it is then delete it
                            if (!containerIsScheduledToRunNow &&
                                DateTime.UtcNow > lastActivity + lingerTime &&
                                runningContainer != null
                               )
                            {
                                Log.Debug($"Deleting running container {runningContainer.Name} for Selection Group {selectionGroup.GroupName}");
                                AllParallelTasks.Add(TerminateContainerAsync(selectionGroup.Id.ToString(), contentToken));
                            }
                            else
                            {
                                Log.Debug($"\tNo running container needs to be deleted for Selection Group {selectionGroup.GroupName}");
                            }

                        }
                        #endregion

                        // 5) Find all "preview" Container Instances NOT associated with a preview-ready publication request
                        List<string> legitimatePreviewContainerNames = pendingPublications.Select(p => p.Id.ToString()).ToList();
                        IEnumerable<(string Name, string Token)> orphanPreviewContainers = previewContainerGroups.Where(cg => !legitimatePreviewContainerNames.Contains(cg.Name))
                                                                                                                 .Select(cg => (cg.Name, cg.Tags["content_token"]));
                        foreach ((string Name, string Token) orphan in orphanPreviewContainers)
                        {
                            Log.Debug($"Deleting orphaned preview container group {orphan.Name}");
                        }

                        // 6) Find all "live" Container Instances NOT associated with a live selection group
                        List<string> legitimateLiveContainerNames = liveSelectionGroups.Select(p => p.Id.ToString()).ToList();
                        IEnumerable<(string Name, string Token)> orphanLiveContainers = liveContainerGroups.Where(g => !legitimateLiveContainerNames.Contains(g.Name))
                                                                                                     .Select(g => (g.Name, g.Tags["content_token"]));
                        foreach ((string Name, string Token) orphan in orphanLiveContainers)
                        {
                            Log.Debug($"Deleting orphaned live container group {orphan.Name}");
                        }

                        // Delete all orphan containers
                        foreach ((string Name, string Token) orphan in orphanPreviewContainers.Concat(orphanLiveContainers))
                        {
                            AllParallelTasks.Add(TerminateContainerAsync(orphan.Name, orphan.Token));
                        }

                        // Wait for all the async things to be done
                        await Task.WhenAll(AllParallelTasks);
                    }
                }
                catch { }

                await Task.Delay(30_000, stoppingToken);
            }
        }

        private async Task RequestContainer(Guid containerGroupNameGuid, RootContentItem contentItem, bool isLiveContent, string contentToken, ContainerGroupResourceTags resourceTags = null)
        {
            ContainerizedAppContentItemProperties typeSpecificInfo = contentItem.TypeSpecificDetailObject as ContainerizedAppContentItemProperties;

            string ipAddressType = _appConfig.GetValue<string>("ContainerContentIpAddressType");
            // use a tuple so that both succeed or both fail
            (string vnetId, string vnetName) = ipAddressType == "Public"
                ? (null, null)
                : (_appConfig.GetValue<string>("ContainerContentVnetId"), _appConfig.GetValue<string>("ContainerContentVnetName"));

            try
            {
                ContainerizedAppLibApi api = await new ContainerizedAppLibApi(_containerizedAppLibApiConfig).InitializeAsync(contentItem.AcrRepoositoryName);

                GlobalFunctions.IssueLog(IssueLogEnum.TrackingContainerPublishing, $"Initiating run of preview container instance for content item ID {contentItem.Id}, publication request ID {containerGroupNameGuid.ToString()}");
                string containerUrl = await api.RunContainer(containerGroupNameGuid.ToString(),
                                                             isLiveContent ? typeSpecificInfo.LiveImageName : typeSpecificInfo.PreviewImageName,
                                                             isLiveContent ? typeSpecificInfo.LiveImageTag : typeSpecificInfo.PreviewImageTag,
                                                             ipAddressType,
                                                             isLiveContent ? (int)typeSpecificInfo.LiveContainerCpuCores : (int)typeSpecificInfo.PreviewContainerCpuCores,
                                                             isLiveContent ? (int)typeSpecificInfo.LiveContainerRamGb : (int)typeSpecificInfo.PreviewContainerRamGb,
                                                             resourceTags,
                                                             vnetId,
                                                             vnetName,
                                                             false,
                                                             isLiveContent ? typeSpecificInfo.LiveContainerInternalPort : typeSpecificInfo.PreviewContainerInternalPort);

                Log.Information($"Container instance started with URL: {containerUrl}");
            }
            catch { }

            GlobalFunctions.ContainerLastActivity[contentToken] = DateTime.UtcNow;
        }

        private async Task TerminateContainerAsync(string containerGroupName, string contentToken)
        {
            // Any subsequent HTTP request will return 502 after this
            _proxyConfigProvider.RemoveExistingRoute(contentToken);

            // Websockets probably function until the container dies
            await _containerizedAppLibApi.DeleteContainerGroup(containerGroupName);

            GlobalFunctions.ContainerLastActivity.Remove(contentToken);
        }
    }
}
