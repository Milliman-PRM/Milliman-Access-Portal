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
                Console.WriteLine("test");

                foreach (var containerGroup in containerGroups)
                {
                    string rawTags = JsonConvert.SerializeObject(containerGroup.Tags);
                    ContainerGroupResourceTags tags = JsonConvert.DeserializeObject<ContainerGroupResourceTags>(rawTags, new ContainerGroupResourceTagJsonConverter());

                    SelectionGroup containerGroupSelectionGroup = await dbContext.SelectionGroup.FindAsync(tags.SelectionGroupId);
                }
            }
        }
    }
}
