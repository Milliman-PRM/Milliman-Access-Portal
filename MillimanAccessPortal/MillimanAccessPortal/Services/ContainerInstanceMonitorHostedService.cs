using AuditLogLib.Services;
using ContainerizedAppLib;
using ContainerizedAppLib.AzureRestApiModels;
using MapDbContextLib.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class ContainerInstanceMonitorHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _appConfig;
        private readonly ContainerizedAppLibApiConfig _containerizedAppLibApiConfig;

        public ContainerInstanceMonitorHostedService(IServiceProvider servicesArg, IConfiguration appConfigArg, IOptions<ContainerizedAppLibApiConfig> containerizedAppLibApiConfigArg)
        {
            _services = servicesArg;
            _appConfig = appConfigArg;
            _containerizedAppLibApiConfig = containerizedAppLibApiConfigArg.Value;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                await FindRunningContainerInstances();
                await Task.Delay(60_000);
            }
        }

        private async Task FindRunningContainerInstances() // todo change name
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                var containerizedAppLibApi = await new ContainerizedAppLibApi(_containerizedAppLibApiConfig).InitializeAsync(null);

                var containerGroups = await containerizedAppLibApi.ListContainerGroupsInResourceGroup();

                foreach (var containerGroup in containerGroups)
                {
                    string rawTags = JsonConvert.SerializeObject(containerGroup.Tags);
                    JsonSerializerSettings tagSerializerSettings = new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Converters = new List<JsonConverter>()
                        {
                            new ContainerGroupResourceTagJsonConverter(),
                        },
                    };
                    ContainerGroupResourceTags tags = JsonConvert.DeserializeObject<ContainerGroupResourceTags>(rawTags, tagSerializerSettings);

                    // TODO: Implement Container Instance lifetime management on different types of instances.
                    #region Lifetime management for Preview Container Instances
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

                    #region Lifetime management for Non-Preview Container Instances
                    SelectionGroup containerGroupSelectionGroup = await dbContext.SelectionGroup.FindAsync(tags.SelectionGroupId);
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
                }
            }
        }
    }
}
