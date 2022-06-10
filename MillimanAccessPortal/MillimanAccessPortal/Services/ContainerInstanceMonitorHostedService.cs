using AuditLogLib.Services;
using ContainerizedAppLib;
using ContainerizedAppLib.AzureRestApiModels;
using MapCommonLib;
using MapDbContextLib.Context;
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
                                                                                              .Where(p => p.RootContentItem.ContentType.TypeEnum == ContentTypeEnum.ContainerApp)
                                                                                              .Where(p => p.RequestStatus == PublicationStatus.Processed)
                                                                                              // .Where(p => p.CreateDateTimeUtc > DateTime.UtcNow - TimeSpan.FromDays(7))   TODO is this a good idea?
                                                                                              .ToListAsync();

                        Log.Debug($"Publications ready for approval: {Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", pendingPublications.Select(p => $"initiated:{p.CreateDateTimeUtc}, content:{p.RootContentItem.ContentName}"))}");

                        List<SelectionGroup> liveSelectionGroups = await _dbContext.SelectionGroup
                                                                                   .Include(p => p.RootContentItem)
                                                                                   .Where(sg => sg.RootContentItem.ContentType.TypeEnum == ContentTypeEnum.ContainerApp)
                                                                                   .Where(sg => _dbContext.ContentPublicationRequest.Any(p => p.RootContentItemId == sg.RootContentItemId
                                                                                                                                           && p.RequestStatus == PublicationStatus.Confirmed))
                                                                                   .ToListAsync();

                        Log.Debug($"Content items with live selection groups: {Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", liveSelectionGroups.Select(g => $"group:{g.GroupName}, content:{g.RootContentItem.ContentName}"))}");

                        #region Manage preview container instances
                        foreach (ContentPublicationRequest publication in pendingPublications)
                        {
                            List<ContainerGroup_GetResponseModel> runningContainers = previewContainerGroups.Where(c => c.Name == publication.Id.ToString())
                                                                                                            .ToList();

                            //if ()
                        }
                        /*
                         * Check if SelectionGroup tags indicate preview
                         * if Selection Group tags indicate this is a preview Container Instance:
                         *   Find active User sessions on Container
                         *     boolean $userHasEngagedWithSessionInTimeoutWindow = true
                         *     number $timeout = amount of time we'll allow preview images to live after last session activity
                         *     foreach active User session on Container
                         *       if user has not engaged with session in $timeout:
                         *         userHasEngagedWithSessionInTimeoutWindow = false
                         *     if !userHasEngagedWithSessionInTimeoutWindow:
                         *       kill preview image
                         */

                        #endregion
                    }
                }
                catch { }


                #region Lifetime management for Non-Preview Container Instances
                //SelectionGroup containerGroupSelectionGroup = await dbContext.SelectionGroup.FindAsync(tags.SelectionGroupId);
                /*
                 * boolean $isWithinHotServiceWindow = GetContainerIsWithinHotServiceWindow(containerGroupSelectionGroup.typeSpecificDetails)
                 * if $isWithinHotServiceWindow:
                     if container is not running:
                       startInstance();
                 * else
                 *   boolean $userHasEngagedWithSessionInTimeoutWindow = true
                 *   number $timeout = amount of time we'll allow preview images to live after last session activity
                 *   foreach active user session on Container
                 *      if user has not engaged with session in $timeout:
                 *          userHasEngagedWithSessionInTimeoutWindow = false
                 *   if !userHasEngagedWithSesionInTimeoutWindow:
                 *     killContainerInstance();
                 * 
                 */
                #endregion

                await Task.Delay(30_000, stoppingToken);
            }
        }
    }
}
