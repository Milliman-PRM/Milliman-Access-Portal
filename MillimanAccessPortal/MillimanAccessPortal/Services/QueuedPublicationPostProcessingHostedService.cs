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
                AllRunningTasks.RemoveAll(t => t.IsCompleted);

                // Retrieve the id of a publication request to post-process
                Guid publicationRequestId = await TaskQueue.DequeueAsync(cancellationToken, 10_000);

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
                string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), no publication request record found for ID {publicationRequestId}";
                Log.Error(Msg);
                throw new ApplicationException(Msg);
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
                string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), unexpected request status {thisPubRequest.RequestStatus.ToString()} for publication request ID {publicationRequestId}";
                Log.Warning(Msg);
                return;  // TODO should this throw?
            }

            // Prepare useful lists of reduction tasks for use below
            List<ContentReductionTask> RelatedReductionTasks = null;
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                RelatedReductionTasks = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == thisPubRequest.Id)
                    .Include(t => t.SelectionGroup)
                    .ToList();
            }
            List<ContentReductionTask> SuccessfulReductionTasks = RelatedReductionTasks
                .Where(t => t.SelectionGroupId.HasValue)
                .Where(t => !t.SelectionGroup.IsMaster)
                .Where(t => t.OutcomeMetadataObj.OutcomeReason == MapDbReductionTaskOutcomeReason.Success)
                .ToList();

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
            List<ContentRelatedFile> newLiveReadyFilesObj = new List<ContentRelatedFile>();
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

                newLiveReadyFilesObj.Add(new ContentRelatedFile
                {
                    Checksum = Crf.Checksum,
                    FileOriginalName = Crf.FileOriginalName,
                    FilePurpose = Crf.FilePurpose,
                    FullPath = TargetFilePath,
                });
            }
            // record the path change in thisPubRequest
            thisPubRequest.LiveReadyFilesObj = newLiveReadyFilesObj;
            using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.ContentPublicationRequest.Update(thisPubRequest);
                dbContext.SaveChanges();
            }

            // PostProcess the output of successful reduction tasks
            foreach (ContentReductionTask relatedTask in SuccessfulReductionTasks)
            {
                // This assignment defines the live file name
                string TargetFileName = ContentTypeSpecificApiBase.GenerateReducedContentFileName(
                                            relatedTask.SelectionGroupId.Value,   // .HasValue is true for tasks in SuccessfulReductionTasks
                                            thisPubRequest.RootContentItemId,
                                            Path.GetExtension(relatedTask.ResultFilePath));
                string TargetFilePath = Path.Combine(tempContentDestinationFolder, TargetFileName);
                File.Copy(relatedTask.ResultFilePath, TargetFilePath);

                // Update reduction task record with revised path
                relatedTask.ResultFilePath = TargetFilePath;
                using (var dbContext = Services.GetRequiredService<ApplicationDbContext>())
                {
                    dbContext.ContentReductionTask.Update(relatedTask);
                    dbContext.SaveChanges();
                }
            }

            // Delete source folder(s)
            const bool RetainFailedReductions = false;  // TODO Improve logic for what to delete
            IEnumerable<ContentReductionTask> tasksOfFoldersToDelete = RetainFailedReductions
                ? RelatedReductionTasks.Except(SuccessfulReductionTasks)
                : SuccessfulReductionTasks;
            HashSet<string> foldersToDelete = tasksOfFoldersToDelete.Select(t => Path.GetDirectoryName(t.ResultFilePath)).ToHashSet();
            foreach (string folderToDelete in foldersToDelete)
            {
                Directory.Delete(folderToDelete, true);
            }

            // update pub status to Processed
            thisPubRequest.RequestStatus = PublicationStatus.Processed;
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
