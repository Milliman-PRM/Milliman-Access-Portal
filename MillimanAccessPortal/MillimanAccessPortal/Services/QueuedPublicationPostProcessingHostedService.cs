/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Postprocessing of publication requests
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
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

            // Validate that a request record exists with the provided Id
            ContentPublicationRequest thisPubRequest = GetCurrentPublicationRequestEntity(publicationRequestId, serviceProvider);
            if (thisPubRequest == null)
            {
                // TODO log something
                throw new ApplicationException($"In QueuedPublicationPostProcessingHostedService.PostProcess, no publication request found for ID {publicationRequestId}");
            }

            // While the request is processing, wait and requery
            while (thisPubRequest.RequestStatus == PublicationStatus.Processing)
            {
                Thread.Sleep(2000);
                thisPubRequest = GetCurrentPublicationRequestEntity(publicationRequestId, serviceProvider);
            }
            // Ensure that the request is ready for post-processing
            if (thisPubRequest.RequestStatus != PublicationStatus.PostProcessReady)
            {
                // TODO log something
                return;  // or throw
            }

            // Prepare a useful list of reduction tasks for below use
            List<ContentReductionTask> SuccessfulReductionTasks = null;
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                SuccessfulReductionTasks = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == thisPubRequest.Id)
                    .Where(t => t.SelectionGroupId.HasValue)
                    .Where(t => !t.SelectionGroup.IsMaster)
                    .Where(t => t.OutcomeMetadataObj.OutcomeReason == MapDbReductionTaskOutcomeReason.Success)
                    .Include(t => t.SelectionGroup)
                    .ToList();
            }

            #region Validation
            // Validate the existence and checksum of each uploaded (non-reduced) file
            foreach (ContentRelatedFile f in thisPubRequest.LiveReadyFilesObj)
            {
                if (!File.Exists(f.FullPath) || !f.ValidateChecksum())
                {
                    // Log validation failure
                    return;
                }
            }
            // Validate the existence and checksum of each successfully reduced file
            foreach (ContentReductionTask relatedTask in SuccessfulReductionTasks)
            {
                if (!File.Exists(relatedTask.ResultFilePath) || 
                    relatedTask.ReducedContentChecksum.ToLower() != GlobalFunctions.GetFileChecksum(relatedTask.ResultFilePath))
                {
                    // Log validation failure
                    return;
                }
            }
            #endregion

            // update pub status to PostProcessing
            thisPubRequest.RequestStatus = PublicationStatus.PostProcessing;
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.ContentPublicationRequest.Update(thisPubRequest);
                dbContext.SaveChanges();
            }

            string tempContentDestinationFolder = Path.Combine(configuration.GetValue<string>("Storage:ContentItemRootPath"),
                                                               thisPubRequest.RootContentItemId.ToString(),
                                                               thisPubRequest.Id.ToString());

            // Ensure an empty destination folder
            try
            {
                Directory.Delete(tempContentDestinationFolder, true);
            }
            catch (DirectoryNotFoundException) { }
            Directory.CreateDirectory(tempContentDestinationFolder);

            // Move uploaded (non-reduced) files for this publication
            foreach (ContentRelatedFile Crf in thisPubRequest.LiveReadyFilesObj)
            {
                // This assignment defines the live file name
                string TargetFileName = ContentTypeSpecificApiBase.GenerateContentFileName(
                                            Crf.FilePurpose, 
                                            Path.GetExtension(Crf.FullPath), 
                                            thisPubRequest.RootContentItemId);
                string TargetFilePath = Path.Combine(tempContentDestinationFolder, TargetFileName);

                // Can move because destination is on same volume as source
                File.Move(Crf.FullPath, TargetFilePath);

                // TODO record the change in thisPubRequest.LiveReadyFilesObj
            }

            // TODO copy reduced files to tempContentDestinationFolder 
            // TODO Delete source folders
            // TODO Update reduction task records with revised path?
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
