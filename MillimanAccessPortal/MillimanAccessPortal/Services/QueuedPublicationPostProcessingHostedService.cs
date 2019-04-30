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
using Microsoft.Extensions.Options;
using PowerBiLib;
using QlikviewLib;
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

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the id of a publication request to post-process
                Guid publicationRequestId = await TaskQueue.DequeueAsync(cancellationToken);
                GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.ExecuteAsync: obtained publication Id <{publicationRequestId}> from the queue, starting to process");

                if (publicationRequestId != Guid.Empty)
                {
                    using (var scope = Services.CreateScope())
                    {
                        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        ContentPublicationRequest thisPubRequest = dbContext.ContentPublicationRequest
                                                                            .Include(r => r.RootContentItem)
                                                                            .ThenInclude(c => c.ContentType)
                                                                            .SingleOrDefault(r => r.Id == publicationRequestId);

                        try
                        {
                            if (thisPubRequest == null)
                            {
                                string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), no publication request record found with ID {publicationRequestId}";
                                throw new ApplicationException(Msg);
                            }
                            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.ExecuteAsync: found the publication request record, request object is: {{@thisPubRequest}}", Serilog.Events.LogEventLevel.Information, thisPubRequest);

                            object typeSpecificConfig = null;
                            switch (thisPubRequest?.RootContentItem?.ContentType?.TypeEnum)
                            {
                                case ContentTypeEnum.Qlikview:
                                    typeSpecificConfig = scope.ServiceProvider.GetRequiredService<IOptions<QlikviewConfig>>().Value;
                                    break;
                                case ContentTypeEnum.PowerBi:
                                    typeSpecificConfig = scope.ServiceProvider.GetRequiredService<IOptions<PowerBiConfig>>().Value;
                                    break;
                            }

                            await PostProcess(thisPubRequest, dbContext, configuration, typeSpecificConfig);
                            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.ExecuteAsync: PostProcess() returned with no exception");
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                Log.Error(e, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, Exception thrown during QueuedPublicationPostProcessingHostedService processing");

                                thisPubRequest.RequestStatus = PublicationStatus.Error;
                                thisPubRequest.StatusMessage = e.Message;
                                foreach (var reduction in dbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == thisPubRequest.Id))
                                {
                                    reduction.ReductionStatus = ReductionStatusEnum.Error;
                                }
                                dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, Exception thrown from exception handler block");
                            }
                        }
                    }
                }
                else
                {
                    string Msg = "Invalid default publication request Id from post-processing queue";
                    Log.Error(Msg);
                }
            }
        }

        /// <summary>
        /// Should throw if the request should be marked with error status.  The caller must log thrown exception messages
        /// </summary>
        /// <param name="publicationRequestId"></param>
        /// <param name="scopedServiceProvider"></param>
        protected async Task PostProcess(ContentPublicationRequest thisPubRequest, ApplicationDbContext dbContext, IConfiguration configuration, object typeSpecificConfig)
        {
            List<PublicationStatus> WaitStatusList = new List<PublicationStatus> { PublicationStatus.Queued, PublicationStatus.Processing };

            RootContentItem contentItem = thisPubRequest.RootContentItem;

            // While the request is processing, wait and requery
            while (WaitStatusList.Contains(thisPubRequest.RequestStatus))
            {
                Thread.Sleep(2000);
                dbContext.Entry(thisPubRequest).State = EntityState.Detached;  // force update from db
                thisPubRequest = dbContext.ContentPublicationRequest.Find(thisPubRequest.Id);
            }
            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.PostProcess: after polling loop completed, request object is: {{@thisPubRequest}}", Serilog.Events.LogEventLevel.Information, thisPubRequest);
            // Ensure that the request is ready for post-processing
            if (thisPubRequest.RequestStatus != PublicationStatus.PostProcessReady)
            {
                string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), unexpected request status {thisPubRequest.RequestStatus.ToString()} for publication request ID {thisPubRequest.Id}";
                Log.Warning(Msg);
                return;
            }

            ContentTypeEnum thisContentType = dbContext.RootContentItem
                                                   .Where(i => i.Id == thisPubRequest.RootContentItemId)
                                                   .Select(i => i.ContentType.TypeEnum)
                                                   .Single();

            // Prepare useful lists of reduction tasks for use below
            List<ContentReductionTask> AllRelatedReductionTasks = dbContext.ContentReductionTask
                .Where(t => t.ContentPublicationRequestId == thisPubRequest.Id)
                .Include(t => t.SelectionGroup)
                .ToList();
            List<ContentReductionTask> SuccessfulReductionTasks = AllRelatedReductionTasks
                .Where(t => t.SelectionGroupId.HasValue)
                .Where(t => !t.SelectionGroup.IsMaster)
                .Where(t => t.OutcomeMetadataObj.OutcomeReason == MapDbReductionTaskOutcomeReason.Success)
                .ToList();
            List<ContentReductionTask> UnsuccessfulReductionTasks = AllRelatedReductionTasks
                .Where(t => t.SelectionGroupId.HasValue)
                .Where(t => !t.SelectionGroup.IsMaster)
                .Where(t => t.OutcomeMetadataObj.OutcomeReason != MapDbReductionTaskOutcomeReason.Success)
                .ToList();

            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.PostProcess: Related reduction tasks queried: {AllRelatedReductionTasks.Count} total, {SuccessfulReductionTasks.Count} successful, {UnsuccessfulReductionTasks.Count} unsuccessful");

            #region Validation
            // Validate the existence and checksum of each uploaded (non-reduced) file
            foreach (ContentRelatedFile crf in thisPubRequest.LiveReadyFilesObj)
            {
                if (!File.Exists(crf.FullPath))
                {
                    string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), file not found: {crf.FullPath}";
                    throw new ApplicationException(Msg);
                }
                else if (!crf.ValidateChecksum())
                {
                    string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), checksum validation failed for file {crf.FullPath}";
                    throw new ApplicationException(Msg);
                }
            }
            // Validate the existence and checksum of each successfully reduced file
            foreach (ContentReductionTask relatedTask in SuccessfulReductionTasks)
            {
                if (!File.Exists(relatedTask.ResultFilePath) || 
                    relatedTask.ReducedContentChecksum.ToLower() != GlobalFunctions.GetFileChecksum(relatedTask.ResultFilePath).ToLower())
                {
                    string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), validation failed for file {relatedTask.ResultFilePath}";
                    throw new ApplicationException(Msg);
                }
            }
            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.PostProcess: validation block completed for request Id <{thisPubRequest.Id}>");
            #endregion

            // update pub status to PostProcessing
            thisPubRequest.RequestStatus = PublicationStatus.PostProcessing;
            dbContext.SaveChanges();

            GlobalFunctions.IssueLog(IssueLogEnum.QueuePostProcessing, $"QueuedPublicationPostProcessingHostedService.PostProcess: status updated to `PostProcessing` for request Id <{thisPubRequest.Id}>");

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
            dbContext.SaveChanges();

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
                dbContext.ContentReductionTask.Update(relatedTask);
            }
            dbContext.SaveChanges();

            switch (thisContentType)
            {
                case ContentTypeEnum.Qlikview:
                    await new QlikviewLibApi(typeSpecificConfig as QlikviewConfig).AuthorizeUserDocumentsInFolderAsync(thisPubRequest.RootContentItemId.ToString());
                    break;
                case ContentTypeEnum.PowerBi:
                    var newMasterFile = thisPubRequest.LiveReadyFilesObj.SingleOrDefault(f => f.FilePurpose.Equals("MasterContent", StringComparison.OrdinalIgnoreCase));
                    if (newMasterFile != null)
                    {
                        PowerBiLibApi api = await new PowerBiLibApi(typeSpecificConfig as PowerBiConfig).InitializeAsync();
                        PowerBiEmbedModel embedProperties = await api.ImportPbixAsync(newMasterFile.FullPath, 
                                                                                      contentItem.ClientId.ToString(), // The related client ID is used as group name
                                                                                      contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties);

                        PowerBiContentItemProperties contentItemProperties = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                        contentItemProperties.PreviewEmbedProperties = embedProperties;
                        contentItem.TypeSpecificDetailObject = contentItemProperties;
                        dbContext.SaveChanges();
                    }
                    break;
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.Html:
                case ContentTypeEnum.FileDownload:
                default:
                    break;
            }

            // Delete source folder(s)
            bool RetainFailedReductionFolders = false;  // this variable is for debugging use
            HashSet<string> foldersToDelete = RetainFailedReductionFolders
                ? SuccessfulReductionTasks.Select(t => Path.GetDirectoryName(t.MasterFilePath))
                                          .Except(UnsuccessfulReductionTasks.Select(t => Path.GetDirectoryName(t.MasterFilePath)))
                                          .ToHashSet()
                : AllRelatedReductionTasks.Select(t => Path.GetDirectoryName(t.MasterFilePath))
                                          .ToHashSet();
            foreach (string folderToDelete in foldersToDelete)
            {
                if (Directory.Exists(folderToDelete))
                {
                    Directory.Delete(folderToDelete, true);
                }
            }

            // update pub status to Processed
            thisPubRequest.RequestStatus = PublicationStatus.Processed;
            dbContext.ContentPublicationRequest.Update(thisPubRequest);
            dbContext.SaveChanges();
        }
    }
}
