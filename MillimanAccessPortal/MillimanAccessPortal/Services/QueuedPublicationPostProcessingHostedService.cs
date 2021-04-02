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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class QueuedPublicationPostProcessingHostedService : BackgroundService
    {
        protected ConcurrentDictionary<Guid,Task> _runningTasks = new ConcurrentDictionary<Guid, Task>();

        public QueuedPublicationPostProcessingHostedService(
            IServiceProvider services,
            IPublicationPostProcessingTaskQueue taskQueue,
            IConfiguration config)
        {
            _services = services;
            _taskQueue = taskQueue;
            _appConfig = config;
        }

        public IServiceProvider _services { get; }
        public IPublicationPostProcessingTaskQueue _taskQueue { get; }
        public IConfiguration _appConfig { get; }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, hosted service is started");

            try
            {
                await AdoptOrphanPublicationsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, Exception thrown during AdoptOrphanPublicationsAsync()");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the id of a new publication request to post-process
                Guid publicationRequestId = await _taskQueue.DequeueAsync(cancellationToken, 2_000);

                if (publicationRequestId != Guid.Empty)
                {
                    GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"Postprocessing task for publication request {publicationRequestId} has been dequeued");
                    _runningTasks.TryAdd(publicationRequestId, PostProcessAsync(publicationRequestId));
                }

                // Log any ContentPublicatioRequest that threw an exception, and update database with error status
                foreach (var kvpWithException in _runningTasks.Where(t => t.Value.IsFaulted))
                {
                    try
                    {
                        Log.Error(kvpWithException.Value.Exception, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, Exception thrown during QueuedPublicationPostProcessingHostedService processing");

                        using (var scope = _services.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            ContentPublicationRequest thisPubRequest = await dbContext.ContentPublicationRequest.SingleOrDefaultAsync(r => r.Id == kvpWithException.Key);

                            thisPubRequest.RequestStatus = PublicationStatus.Error;

                            var newOutcome = thisPubRequest.OutcomeMetadataObj;
                            newOutcome.ElapsedTime = DateTime.UtcNow - newOutcome.StartDateTime;
                            newOutcome.UserMessage = thisPubRequest.RequestStatus.GetDisplayDescriptionString();
                            newOutcome.SupportMessage = kvpWithException.Value.Exception.Message;
                            thisPubRequest.OutcomeMetadataObj = newOutcome;

                            thisPubRequest.StatusMessage = kvpWithException.Value.Exception.Message;
                            foreach (var reduction in dbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == thisPubRequest.Id))
                            {
                                reduction.ReductionStatus = ReductionStatusEnum.Error;
                            }
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "QueuedPublicationPostProcessingHostedService.ExecuteAsync, Exception thrown from exception handler block");
                    }
                }

                // Stop tracking completed items
                foreach (var completedKvp in _runningTasks.Where(t => t.Value.IsCompleted))
                {
                    GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"Postprocessing thread completed for request ID {completedKvp.Key}");
                    _runningTasks.Remove(completedKvp.Key, out _);
                }
            }
        }

        protected async Task PostProcessAsync(Guid publicationRequestId)
        {
            await Task.Yield();

            using (var scope = _services.CreateScope())
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                List<PublicationStatus> WaitStatusList = new List<PublicationStatus> { PublicationStatus.Queued, PublicationStatus.Processing };

                ContentPublicationRequest thisPubRequest = await dbContext.ContentPublicationRequest
                                                                          .Include(r => r.RootContentItem)
                                                                          .ThenInclude(c => c.ContentType)
                                                                          .SingleOrDefaultAsync(r => r.Id == publicationRequestId);

                RootContentItem contentItem = thisPubRequest.RootContentItem;

                int loopCounter = 0;
                // While the request is processing, wait and requery
                while (WaitStatusList.Contains(thisPubRequest.RequestStatus))
                {
                    if (loopCounter++ % 100 == 0)
                    {
                        GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"At loopCounter {loopCounter}, postprocessing publication request {publicationRequestId} is polling for status in WaitStatusList, found status {thisPubRequest.RequestStatus.GetDisplayNameString()}");
                    }

                    Thread.Sleep(2_000);
                    dbContext.Entry(thisPubRequest).State = EntityState.Detached;  // force update from db
                    thisPubRequest = await dbContext.ContentPublicationRequest.FindAsync(thisPubRequest.Id);
                }
                GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"At loopCounter {loopCounter}, postprocessing publication request {publicationRequestId}, status no longer in WaitStatusList, found status {thisPubRequest.RequestStatus.GetDisplayNameString()}");

                // Ensure that the request is ready for post-processing
                if (thisPubRequest.RequestStatus != PublicationStatus.PostProcessReady)
                {
                    string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), expected request status PostProcessReady but found {thisPubRequest.RequestStatus.ToString()} for publication request ID {thisPubRequest.Id}";
                    Log.Warning(Msg);
                    return;
                }

                ContentTypeEnum thisContentType = await dbContext.RootContentItem
                                                                 .Where(i => i.Id == thisPubRequest.RootContentItemId)
                                                                 .Select(i => i.ContentType.TypeEnum)
                                                                 .SingleAsync();

                // Prepare useful lists of reduction tasks for use below
                List<ContentReductionTask> AllRelatedReductionTasks = await dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == thisPubRequest.Id)
                    .Include(t => t.SelectionGroup)
                    .ToListAsync();
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

                #region Validation
                // Validate the existence and checksum of each uploaded (non-reduced) file
                foreach (ContentRelatedFile crf in thisPubRequest.LiveReadyFilesObj)
                {
                    if (!File.Exists(crf.FullPath))
                    {
                        string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), related file not found: {crf.FullPath}";
                        throw new ApplicationException(Msg);
                    }
                    else if (!crf.ValidateChecksum())
                    {
                        string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), checksum validation failed for related file {crf.FullPath}";
                        throw new ApplicationException(Msg);
                    }
                }
                // Validate the existence and checksum of each uploaded associated file
                foreach (ContentAssociatedFile caf in thisPubRequest.LiveReadyAssociatedFilesList)
                {
                    if (!File.Exists(caf.FullPath))
                    {
                        string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), associated file not found: {caf.FullPath}";
                        throw new ApplicationException(Msg);
                    }
                    else if (!caf.ValidateChecksum())
                    {
                        string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), checksum validation failed for associated file {caf.FullPath}";
                        throw new ApplicationException(Msg);
                    }
                }
                // Validate the existence and checksum of each successfully reduced file
                foreach (ContentReductionTask relatedTask in SuccessfulReductionTasks)
                {
                    if (File.Exists(relatedTask.ResultFilePath))
                    {
                        (string checksum, long length) = GlobalFunctions.GetFileChecksum(relatedTask.ResultFilePath);
                        if (!relatedTask.ReducedContentChecksum.Equals(checksum))
                        {
                            string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), length {length}, checksum {checksum} found for file {relatedTask.ResultFilePath}";
                            throw new ApplicationException(Msg);
                        }
                    }
                    else
                    {
                        string Msg = $"In QueuedPublicationPostProcessingHostedService.PostProcess(), file not found: {relatedTask.ResultFilePath}";
                        throw new ApplicationException(Msg);
                    }
                }
                #endregion

                // update pub status to PostProcessing
                thisPubRequest.RequestStatus = PublicationStatus.PostProcessing;

                var newOutcome = thisPubRequest.OutcomeMetadataObj;
                newOutcome.ElapsedTime = DateTime.UtcNow - newOutcome.StartDateTime;
                newOutcome.UserMessage = thisPubRequest.RequestStatus.GetDisplayDescriptionString();
                thisPubRequest.OutcomeMetadataObj = newOutcome;

                await dbContext.SaveChangesAsync();
                GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, $"Postprocessing task for publication request {publicationRequestId} updated to status PostProcessing");

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

                // Move uploaded (non-reduced) related files for this publication
                List<ContentRelatedFile> newLiveReadyFilesObj = new List<ContentRelatedFile>();
                foreach (ContentRelatedFile Crf in thisPubRequest.LiveReadyFilesObj)
                {
                    // This assignment defines the live file name, used in the preview folder for the pub request
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

                // Move uploaded associated files for this publication
                List<ContentAssociatedFile> newLiveReadyAssociatedFilesList = new List<ContentAssociatedFile>();
                foreach (ContentAssociatedFile Caf in thisPubRequest.LiveReadyAssociatedFilesList)
                {
                    // This assignment defines the live file name, used in the preview folder for the pub request
                    string TargetFileName = ContentTypeSpecificApiBase.GenerateLiveAssociatedFileName(
                                                Caf.Id,
                                                thisPubRequest.RootContentItemId,
                                                Path.GetExtension(Caf.FullPath));
                    string TargetFilePath = Path.Combine(tempContentDestinationFolder, TargetFileName);

                    // Can move because destination is on same volume as source
                    File.Move(Caf.FullPath, TargetFilePath);

                    newLiveReadyAssociatedFilesList.Add(new ContentAssociatedFile
                    {
                        Id = Caf.Id,
                        Checksum = Caf.Checksum,
                        FileOriginalName = Caf.FileOriginalName,
                        DisplayName = Caf.DisplayName,
                        FullPath = TargetFilePath,
                        FileType = Caf.FileType,
                        SortOrder = Caf.SortOrder,
                    });
                }

                // record the path change in thisPubRequest
                thisPubRequest.LiveReadyFilesObj = newLiveReadyFilesObj;
                thisPubRequest.LiveReadyAssociatedFilesList = newLiveReadyAssociatedFilesList;
                await dbContext.SaveChangesAsync();

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
                }
                await dbContext.SaveChangesAsync();

                switch (thisContentType)
                {
                    case ContentTypeEnum.Qlikview:
                        QlikviewConfig qvConfig = scope.ServiceProvider.GetRequiredService<IOptions<QlikviewConfig>>().Value;
                        await new QlikviewLibApi(qvConfig).AuthorizeUserDocumentsInFolderAsync(thisPubRequest.RootContentItemId.ToString());
                        break;
                    case ContentTypeEnum.PowerBi:
                        PowerBiConfig pbiConfig = scope.ServiceProvider.GetRequiredService<IOptions<PowerBiConfig>>().Value;
                        var newMasterFile = thisPubRequest.LiveReadyFilesObj.SingleOrDefault(f => f.FilePurpose.Equals("MasterContent", StringComparison.OrdinalIgnoreCase));
                        if (newMasterFile != null)
                        {
                            PowerBiLibApi api = await new PowerBiLibApi(pbiConfig).InitializeAsync();
                            PowerBiEmbedModel embedProperties = await api.ImportPbixAsync(newMasterFile.FullPath, contentItem.ClientId.ToString()); // The related client ID is used as group name

                            PowerBiContentItemProperties contentItemProperties = contentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                            contentItemProperties.PreviewWorkspaceId = embedProperties.WorkspaceId;
                            contentItemProperties.PreviewEmbedUrl = embedProperties.EmbedUrl;
                            contentItemProperties.PreviewReportId = embedProperties.ReportId;

                            contentItem.TypeSpecificDetailObject = contentItemProperties;
                            await dbContext.SaveChangesAsync();
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

                newOutcome = thisPubRequest.OutcomeMetadataObj;
                newOutcome.ElapsedTime = DateTime.UtcNow - newOutcome.StartDateTime;
                newOutcome.UserMessage = thisPubRequest.RequestStatus.GetDisplayDescriptionString();
                thisPubRequest.OutcomeMetadataObj = newOutcome;

                await dbContext.SaveChangesAsync();
            }
        }

        protected async Task AdoptOrphanPublicationsAsync()
        {
            int recoveryLookbackHours = _appConfig.GetValue("TaskRecoveryLookbackHours", 24 * 7);
            DateTime minCreateDateTimeUtc = DateTime.UtcNow - TimeSpan.FromHours(recoveryLookbackHours);

            using (var scope = _services.CreateScope())
            {
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 1) prepare to postprocess publication requests that publishing server could be working with or finished with
                List<PublicationStatus> queuedOrLaterOrphanStatusList = new List<PublicationStatus>
                {
                    PublicationStatus.Queued,
                    PublicationStatus.Processing,
                    PublicationStatus.PostProcessReady,
                };

                List<ContentPublicationRequest> recentOrphanedRequests = await dbContext.ContentPublicationRequest
                    .Where(r => queuedOrLaterOrphanStatusList.Contains(r.RequestStatus))
                    .Where(r => r.CreateDateTimeUtc > minCreateDateTimeUtc)
                    .ToListAsync();

                var latestOrphanedRequests = recentOrphanedRequests
                    .GroupBy(keySelector: r => r.RootContentItemId,
                             resultSelector: (rcid, group) => group.Aggregate(seed: group.First(), func: (prev, next) => prev.CreateDateTimeUtc > next.CreateDateTimeUtc ? prev : next))
                    .ToList();
                foreach (var request in latestOrphanedRequests)
                {
                    _taskQueue.QueuePublicationPostProcess(request.Id);
                }

                // 2) handle publication requests with Validating status
                string CxnString = _appConfig.GetConnectionString("DefaultConnection");  // key string must match that used in startup.cs
                string rootPath = _appConfig.GetSection("Storage")["ContentItemRootPath"];
                string exchangePath = _appConfig.GetSection("Storage")["MapPublishingServerExchangePath"];

                List<ContentPublicationRequest> validatingRequests = await dbContext.ContentPublicationRequest
                    .Where(r => r.RequestStatus == PublicationStatus.Validating)
                    .Where(r => r.CreateDateTimeUtc > minCreateDateTimeUtc)
                    .ToListAsync();
                foreach (ContentPublicationRequest request in validatingRequests)
                {
                    await ContentPublishSupport.MonitorPublicationRequestForQueueingAsync(request.Id, CxnString, rootPath, exchangePath, _taskQueue);
                }

            }
        }
    }
}
