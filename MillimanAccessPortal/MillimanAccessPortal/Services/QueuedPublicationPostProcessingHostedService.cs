/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Postprocessing of publication requests
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Storage;  // for transaction support
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class QueuedPublicationPostProcessingHostedService : BackgroundService
    {
        public QueuedPublicationPostProcessingHostedService(
            IServiceProvider services,
            IPublicationPostProcessingTaskQueue taskQueue)
        {
            Services = services;
            TaskQueue = taskQueue;
        }

        public IServiceProvider Services { get; }
        public IPublicationPostProcessingTaskQueue TaskQueue { get; }

        protected List<Task> AllRunningTasks = new List<Task>();

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // This collection might get stale while waiting for another task, but that's ok. 
                AllRunningTasks.RemoveAll(t => t.IsCompleted);

                // Retrieve the id of the publication request to post-process
                Guid publicationRequestId = await TaskQueue.DequeueAsync(cancellationToken);

                if (publicationRequestId != Guid.Empty)
                {
                    AllRunningTasks.Add
                    (
                        Task.Run(() => PostProcess(publicationRequestId, Services))
                    );
                }
            }
        }

        protected void PostProcess(Guid publicationRequestId, IServiceProvider serviceProvider)
        {
            //var auditLogger = serviceProvider.GetRequiredService<IAuditLogger>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            ContentPublicationRequest thisPubRequest = GetCurrentPublicationRequestEntity(publicationRequestId, serviceProvider);

            if (thisPubRequest == null)
            {
                throw new ApplicationException($"In QueuedPublicationPostProcessingHostedService.PostProcess, no publication request found for ID {publicationRequestId}");
            }

            // Wait while the request is processing
            while (thisPubRequest.RequestStatus == PublicationStatus.Processing)
            {
                Thread.Sleep(2000);
                thisPubRequest = GetCurrentPublicationRequestEntity(publicationRequestId, serviceProvider);
            }

            if (thisPubRequest.RequestStatus != PublicationStatus.PostProcessReady)
            {
                return;
            }

            string tempContentDestinationFolder = Path.Combine(configuration.GetValue<string>("Storage:ContentItemRootPath"), 
                                                               thisPubRequest.RootContentItemId.ToString(), 
                                                               thisPubRequest.Id.ToString());

            // update pub status to PostProcessing
            thisPubRequest.RequestStatus = PublicationStatus.PostProcessing;
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.ContentPublicationRequest.Update(thisPubRequest);
                dbContext.SaveChanges();
            }

        }

        protected ContentPublicationRequest GetCurrentPublicationRequestEntity(Guid publicationRequestId, IServiceProvider serviceProvider)
        {
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                ContentPublicationRequest thisPubRequest = dbContext.ContentPublicationRequest.SingleOrDefault(r => r.Id == publicationRequestId);
                return thisPubRequest;
            }
        }
    }
}
