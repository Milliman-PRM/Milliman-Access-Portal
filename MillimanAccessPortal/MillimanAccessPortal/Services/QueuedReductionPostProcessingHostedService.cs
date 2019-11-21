/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A hosted service that performs postprocessing of out-of-process reductions due to selection updates
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using QlikviewLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class QueuedReductionPostProcessingHostedService : BackgroundService
    {
        public QueuedReductionPostProcessingHostedService(
            IServiceProvider services,
            IConfiguration config)
        {
            _services = services;
            _appConfig = config;
        }

        public IServiceProvider _services { get; }
        public IConfiguration _appConfig { get; }

        public async override Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => AdoptOrphanReductions());
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        protected void AdoptOrphanReductions()
        {
            int publishingRecoveryLookbackHours = _appConfig.GetValue("publishingRecoveryLookbackHours", 24 * 7);
            DateTime minCreateDateTimeUtc = DateTime.UtcNow - TimeSpan.FromHours(publishingRecoveryLookbackHours);

            string ContentItemRootPath = _appConfig.GetValue<string>("ContentItemRootPath");

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                string dbCxnString = dbContext.Database.GetDbConnection().ConnectionString;

                // First setup a go-live handler for tasks already queued or beyond
                var reducedTasks = dbContext.ContentReductionTask
                    .Include(t => t.SelectionGroup)
                        .ThenInclude(g => g.RootContentItem)
                            .ThenInclude(c => c.ContentType)
                    .Where(t => t.ContentPublicationRequestId == null)
                    .Where(t => t.CreateDateTimeUtc > minCreateDateTimeUtc)
                    .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued
                             || t.ReductionStatus == ReductionStatusEnum.Reducing
                             || t.ReductionStatus == ReductionStatusEnum.Reduced);

                foreach (ContentReductionTask task in reducedTasks)
                {
                    object contentTypeConfigObj = null;
                    switch (task.SelectionGroup.RootContentItem.ContentType.TypeEnum)
                    {
                        case ContentTypeEnum.Qlikview:
                            contentTypeConfigObj = scope.ServiceProvider.GetRequiredService<IOptions<QlikviewConfig>>().Value;
                            break;
                    }

                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    ContentAccessSupport.AddReductionMonitor(
                        Task.Run(() => ContentAccessSupport.MonitorReductionTaskForGoLive(task.Id, dbCxnString, ContentItemRootPath, contentTypeConfigObj, cancellationTokenSource.Token), cancellationTokenSource.Token));

                    // Second, (if possible) perform long running processing for reductions that have not become queued yet
                }

            }
        }
    }
}
