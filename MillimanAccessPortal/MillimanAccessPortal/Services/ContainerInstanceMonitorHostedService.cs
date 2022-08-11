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
        private readonly TimeSpan _warmSchedulePaddingTime = TimeSpan.FromMinutes(5);

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

                        string DatabaseUniqueId = _dbContext.NameValueConfiguration.Single(c => c.Key == NewGuidValueKeys.DatabaseInstanceGuid.GetDisplayNameString(false)).Value;

                        List<ContainerGroup_GetResponseModel> allContainerGroups = (await _containerizedAppLibApi.ListContainerGroupsInResourceGroup())
                                                                                                                 .Where(g => g.Tags.ContainsKey("database_id")
                                                                                                                          && g.Tags["database_id"].Equals(DatabaseUniqueId))
                                                                                                                 .ToList();
                        List<ContainerGroup_GetResponseModel> previewContainerGroups = allContainerGroups.Where(cg => cg.Tags.ContainsKey("publicationRequestId"))
                                                                                                         .ToList();
                        List<ContainerGroup_GetResponseModel> liveContainerGroups = allContainerGroups.Where(cg => cg.Tags.ContainsKey("selectionGroupId")).ToList();

                        IProxyConfig proxyConfig = _proxyConfigProvider.GetConfig();

                        #region Manage preview container instances
                        List<ContentPublicationRequest> allActivePublications = await _dbContext.ContentPublicationRequest
                                                                                                .Include(p => p.RootContentItem)
                                                                                                    .ThenInclude(rc => rc.Client)
                                                                                                        .ThenInclude(c => c.ProfitCenter)
                                                                                                .Where(p => p.RootContentItem.ContentType.TypeEnum == ContentTypeEnum.ContainerApp)
                                                                                                .Where(p => PublicationStatusExtensions.ActiveStatuses.Contains(p.RequestStatus))
                                                                                                // .Where(p => p.CreateDateTimeUtc > DateTime.UtcNow - TimeSpan.FromDays(7))   TODO is this a good idea?
                                                                                                .ToListAsync();
                        List<ContentPublicationRequest> pendingPublications = allActivePublications.Where(p => p.RequestStatus == PublicationStatus.Processed)
                                                                                                   .ToList();

                        foreach (ContentPublicationRequest publication in pendingPublications)
                        {
                            ContainerGroup_GetResponseModel runningContainer = previewContainerGroups.SingleOrDefault(c => c.Name == publication.Id.ToString());

                            string contentToken = GlobalFunctions.HexMd5String(publication.Id);

                            DateTime lastActivity = GlobalFunctions.ContainerLastActivity.GetValueOrDefault(contentToken, DateTime.MinValue);

                            #region 1) If a preview container should be running
                            if (publication.RequestStatus == PublicationStatus.Processed &&
                                DateTime.UtcNow < publication.OutcomeMetadataObj.StartDateTime + _previewContainerLingerTime ||
                                DateTime.UtcNow < lastActivity + _previewContainerLingerTime)
                            {
                                // 1a) If no container is running then launch one
                                if (runningContainer is null)
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
                                        DatabaseId = DatabaseUniqueId,
                                    };

                                    Log.Information($"Container lifetime service launching container for preview of content item <{publication.RootContentItem.ContentName}> (ID {publication.RootContentItemId}), publication request <{publication.Id}>");

                                    AllParallelTasks.Add(RequestContainer(publication.Id, publication.RootContentItem, false, contentToken, tags));
                                }
                                else
                                {
                                    // 1b) If container is fully started but there is no matching proxy config then add one
                                    if (runningContainer.Uri is not null && GlobalFunctions.MapUriRoot is not null)
                                    {
                                        if (!proxyConfig.Routes.Any(r => r.RouteId.StartsWith($"/{contentToken}")))
                                        {
                                            UriBuilder externalRequestUri = new UriBuilder
                                            {
                                                Scheme = GlobalFunctions.MapUriRoot.Scheme,
                                                Host = GlobalFunctions.MapUriRoot.Host,
                                                Port = GlobalFunctions.MapUriRoot.Port,
                                                Path = $"/{contentToken}/",  // must include trailing '/' character
                                            };

                                            // Add a new YARP route/cluster config
                                            _proxyConfigProvider.AddNewRoute(contentToken, externalRequestUri.Uri.AbsoluteUri, runningContainer.Uri.AbsoluteUri);
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region 2) If a preview container should NOT be running and it is then delete it
                            if (publication.RequestStatus == PublicationStatus.Processed &&
                                DateTime.UtcNow > publication.OutcomeMetadataObj.StartDateTime + TimeSpan.FromMinutes(30) &&
                                DateTime.UtcNow > lastActivity + _previewContainerLingerTime &&
                                runningContainer?.Uri is not null
                               )
                            {
                                Log.Information($"Container lifetime service deleting container for preview of content item <{publication.RootContentItem.ContentName}> (ID {publication.RootContentItemId}), publication request <{publication.Id}>");

                                AllParallelTasks.Add(TerminateContainerAsync(publication.Id.ToString(), contentToken));
                            }
                            #endregion
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

                        #region Manage live selection group container instances
                        foreach (SelectionGroup selectionGroup in liveSelectionGroups)
                        {
                            ContainerizedAppContentItemProperties contentItemProps = selectionGroup.RootContentItem.TypeSpecificDetailObject as ContainerizedAppContentItemProperties;
                            bool containerIsScheduledToRunNow = false;
                            TimeSpan lingerTime = contentItemProps.LiveContainerLifetimeScheme.ContainerLingerTimeAfterActivity;

                            if (contentItemProps.LiveContainerLifetimeScheme.Scheme == ContainerInstanceLifetimeSchemeEnum.Custom) {
                                ContainerizedAppContentItemProperties.CustomScheduleLifetimeScheme customScheme = (ContainerizedAppContentItemProperties.CustomScheduleLifetimeScheme)contentItemProps.LiveContainerLifetimeScheme;
                                containerIsScheduledToRunNow = customScheme.IsScheduledOnNow(_warmSchedulePaddingTime);
                            }

                            ContainerGroup_GetResponseModel runningContainer = liveContainerGroups.SingleOrDefault(c => c.Name == selectionGroup.Id.ToString());
                            string contentToken = GlobalFunctions.HexMd5String(selectionGroup.Id);

                            DateTime lastActivity = GlobalFunctions.ContainerLastActivity.GetValueOrDefault(contentToken, DateTime.MinValue);

                            // Log.Debug($"\tFor selection group <{selectionGroup.GroupName}>: lastActivity={lastActivity}, containerIsScheduledToRunNow={containerIsScheduledToRunNow}, lingerTime={lingerTime}, running container=<{runningContainer?.Name ?? "none"}>");

                            #region 3) If a live container should be running
                            if ((containerIsScheduledToRunNow || DateTime.UtcNow < lastActivity + lingerTime) &&
                                runningContainer == null &&
                                !selectionGroup.IsSuspended &&
                                !selectionGroup.RootContentItem.IsSuspended
                               )
                            {
                                // 3a) no container exists
                                if (runningContainer is null)
                                {
                                    Log.Information($"Container lifetime service launching container for live content item <{selectionGroup.RootContentItem.ContentName}> (ID {selectionGroup.RootContentItemId}), selection group <{selectionGroup.GroupName}> (ID {selectionGroup.Id}).  Last activity time: {lastActivity}, scheduled now: {containerIsScheduledToRunNow}");
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
                                        DatabaseId = DatabaseUniqueId,
                                    };
                                    AllParallelTasks.Add(RequestContainer(selectionGroup.Id, selectionGroup.RootContentItem, true, contentToken, tags));
                                }
                                else
                                {
                                    // 3b) If container exists but there is no matching proxy config then add one
                                    if (runningContainer.Properties.IpAddress is not null &&
                                        GlobalFunctions.MapUriRoot is not null)
                                    {
                                        if (!proxyConfig.Routes.Any(r => r.RouteId.StartsWith($"/{contentToken}")))
                                        {
                                            UriBuilder externalRequestUri = new UriBuilder
                                            {
                                                Scheme = GlobalFunctions.MapUriRoot.Scheme,
                                                Host = GlobalFunctions.MapUriRoot.Host,
                                                Port = GlobalFunctions.MapUriRoot.Port,
                                                Path = $"/{contentToken}/",  // must include trailing '/' character
                                            };

                                            // Add a new YARP route/cluster config
                                            _proxyConfigProvider.AddNewRoute(contentToken, externalRequestUri.Uri.AbsoluteUri, runningContainer.Uri.AbsoluteUri);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Log.Debug($"\tA container {(runningContainer == null ? "is not expected now" : "is already running")}");
                            }
                            #endregion

                            #region 4) If a live container should NOT be running and it is then delete it
                            if ((!containerIsScheduledToRunNow &&
                                  DateTime.UtcNow > lastActivity + lingerTime &&
                                  runningContainer != null) ||
                                selectionGroup.IsSuspended ||
                                selectionGroup.RootContentItem.IsSuspended
                               )
                            {
                                Log.Information($"Container lifetime service deleting container for live content item <{selectionGroup.RootContentItem.ContentName}> (ID {selectionGroup.RootContentItemId}), selection group <{selectionGroup.GroupName}> (ID {selectionGroup.Id}).  Last activity time: {lastActivity}, scheduled now: {containerIsScheduledToRunNow}");
                                AllParallelTasks.Add(TerminateContainerAsync(selectionGroup.Id.ToString(), contentToken));
                            }
                            else
                            {
                                // Log.Debug($"\tNo running container needs to be deleted for Selection Group <{selectionGroup.GroupName}>, Content Item <{selectionGroup.RootContentItem.ContentName}>");
                            }
                            #endregion
                        }
                        #endregion

                        #region 5) Terminate all "preview" Container Instances NOT associated with a preview-ready publication request
                        List<string> nonOrphanPreviewContainerNames = allActivePublications.Select(p => p.Id.ToString()).ToList();
                        IEnumerable<(string Name, Dictionary<string, string> Tags)> orphanPreviewContainers = previewContainerGroups.Where(cg => !nonOrphanPreviewContainerNames.Contains(cg.Name))
                                                                                                                                    .Where(cg => cg.Uri is not null)
                                                                                                                                    .Select(cg => (cg.Name, cg.Tags));
                        foreach ((string Name, Dictionary<string, string> Tags) orphan in orphanPreviewContainers)
                        {
                            Log.Information($"Container lifetime service deleting orphaned preview container group {orphan.Name} with tags: <{string.Join(", ", orphan.Tags.Select(t => $"{t.Key}:{t.Value}"))}>");
                            AllParallelTasks.Add(TerminateContainerAsync(orphan.Name, orphan.Tags["content_token"]));
                        }
                        #endregion

                        #region 6) Terminate all "live" Container Instances NOT associated with a live selection group
                        List<string> legitimateLiveContainerNames = liveSelectionGroups.Select(p => p.Id.ToString()).ToList();
                        IEnumerable<(string Name, Dictionary<string,string> Tags)> orphanLiveContainers = liveContainerGroups.Where(g => !legitimateLiveContainerNames.Contains(g.Name))
                                                                                                     .Select(g => (g.Name, g.Tags));
                        foreach ((string Name, Dictionary<string, string> Tags) orphan in orphanLiveContainers)
                        {
                            Log.Information($"Container lifetime service deleting orphaned live container group {orphan.Name} with tags: <{string.Join(", ", orphan.Tags.Select(t => $"{t.Key}:{t.Value}"))}>");
                            AllParallelTasks.Add(TerminateContainerAsync(orphan.Name, orphan.Tags["content_token"]));
                        }
                        #endregion

                        // Wait for all the parallel async things to be done
                        await Task.WhenAll(AllParallelTasks);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while processing container lifetime logic");
                }

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

                GlobalFunctions.ContainerLastActivity.AddOrUpdate(contentToken, DateTime.UtcNow, (_,_) => DateTime.UtcNow);

                GlobalFunctions.IssueLog(IssueLogEnum.TrackingContainerPublishing, $"Starting new container instance for content item {contentItem.ContentName}, container name {containerGroupNameGuid}");
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
                                                             new Dictionary<string, string> { { "PathBase", contentToken } },
                                                             isLiveContent ? typeSpecificInfo.LiveContainerInternalPort : typeSpecificInfo.PreviewContainerInternalPort);

                Log.Information($"Container instance started with URL: {containerUrl}");
            }
            catch { }
        }

        /// <summary>
        /// For a named container in ACI: removes the Routes/Cluster from YARP proxy configuration, deletes the container instance, and forgets the last activity time
        /// </summary>
        /// <param name="containerGroupName"></param>
        /// <param name="contentToken"></param>
        /// <returns></returns>
        private async Task TerminateContainerAsync(string containerGroupName, string contentToken)
        {
            // Any subsequent HTTP request will return 502 after this
            _proxyConfigProvider.RemoveExistingRoute(contentToken);

            // Websockets probably function until the container dies
            await _containerizedAppLibApi.DeleteContainerGroup(containerGroupName);

            GlobalFunctions.ContainerLastActivity.Remove(contentToken, out _);
        }
    }
}
