/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Functionality to support actions in the ContentPublishingController
 * DEVELOPER NOTES: Strongly consider utilizing a background task queue for this in the future.
 *      See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MillimanAccessPortal.Models.ContentPublishing;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal
{
    internal class ContentPublishSupport
    {
        private static List<Task> RunningPublicationMonitors = new List<Task>();
        private static Timer CleanupTimer = null;

        internal static void AddPublicationMonitor(Task NewTask)
        {
            lock(RunningPublicationMonitors)
            {
                RunningPublicationMonitors.Add(NewTask);
                if (CleanupTimer == null)
                {
                    CleanupTimer = new Timer(Cleanup, null, 2000, 2000);
                }
            }
        }

        private static void Cleanup(object state)
        {
            lock(RunningPublicationMonitors)
            {
                int CompletedTask = Task.WaitAny(RunningPublicationMonitors.ToArray(), 10);
                if (CompletedTask == -1)
                {
                    return;
                }

                // A task is completed
                RunningPublicationMonitors.RemoveAt(CompletedTask);

                if (RunningPublicationMonitors.Count == 0)
                {
                    CleanupTimer.Dispose();
                    CleanupTimer = null;
                }
            }
        }

        internal static async Task MonitorPublicationRequestForQueueingAsync(Guid publicationRequestId, 
                                                                  string connectionString, 
                                                                  string contentItemRootPath, 
                                                                  string exchangePath, 
                                                                  IPublicationPostProcessingTaskQueue postProcessingTaskQueue)
        {
            DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
            DbContextOptions<ApplicationDbContext> ContextOptions = ContextBuilder.Options;

            #region Wait till all uploads are "valid"
            bool validationWindowComplete = false;

            ContentPublicationRequest publicationRequest;
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                publicationRequest = await Db.ContentPublicationRequest
                                            .SingleAsync(r => r.Id == publicationRequestId);
            }

            // TODO some day when we actually support associated files, is the associated file list f.Id usage below correct?  (e.g. is the Id field equivalent to a FileUpload id?)
            List<Guid> fileUploadIds = publicationRequest.UploadedRelatedFilesObj.Select(f => f.FileUploadId).Union(
                                       publicationRequest.RequestedAssociatedFileList.Select(f => f.Id))
                                       .ToList();

            do
            {
                Thread.Sleep(2000);
                DateTime queueWhenOlderThanDateTimeUtc = DateTime.UtcNow - TimeSpan.FromSeconds(GlobalFunctions.VirusScanWindowSeconds);

                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    List<FileUpload> fileUploadRecords = await Db.FileUpload
                                                                        .Where(u => fileUploadIds.Contains(u.Id))
                                                                        .ToListAsync();

                    if (fileUploadRecords.Count < fileUploadIds.Count)
                    {
                        List<Guid> idsOfMissingRecords = fileUploadIds.Except(fileUploadRecords.Select(u => u.Id)).ToList();

                        string msg = $"While waiting for file uploads to satisfy validation criteria, expected FileUpload record(s) <{string.Join(",", idsOfMissingRecords)}> were not found.  Found {fileUploadRecords.Count} related records";
                        publicationRequest.RequestStatus = PublicationStatus.Error;
                        publicationRequest.StatusMessage = msg;
                        await Db.SaveChangesAsync();
                        Log.Error(msg);
                        return;
                    }

                    validationWindowComplete = fileUploadRecords.All(u => u.CreatedDateTimeUtc < queueWhenOlderThanDateTimeUtc);
                }
            }
            while (!validationWindowComplete);
            #endregion

            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                publicationRequest = await Db.ContentPublicationRequest
                                                .Include(p => p.RootContentItem)
                                                    .ThenInclude(c => c.ContentType)
                                                .SingleAsync(r => r.Id == publicationRequestId);

                Guid ThisRequestGuid = Guid.NewGuid();

                var publicationStatus = publicationRequest?.RequestStatus ?? PublicationStatus.Canceled;
                if (publicationStatus == PublicationStatus.Canceled)
                {
                    return;
                }

                var relatedFiles = publicationRequest.UploadedRelatedFilesObj;
                switch (publicationRequest.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.FileDownload:
                        // Only one mastercontent file is supported with this content type
                        if (relatedFiles.Select(f => f.FilePurpose).Count(p => p.ToLower() == "mastercontent") > 1)
                        {
                            throw new ApplicationException("This publication request cannot contain multiple MasterContent files");
                        }

                        foreach (UploadedRelatedFile UploadedFileRef in relatedFiles)
                        {
                            // move uploaded file(s) to content folder with temporary name(s)
                            ContentRelatedFile Crf = default;
                            try
                            {
                                Crf = await HandleRelatedFile(Db, UploadedFileRef, publicationRequest.RootContentItem, publicationRequestId, contentItemRootPath);
                            }
                            catch (Exception ex)
                            {
                                publicationRequest = await Db.ContentPublicationRequest.FindAsync(publicationRequest.Id);
                                Log.Error(ex, $"Exception from HandleRelatedFile.ContentPublishSupport");
                                publicationRequest.RequestStatus = PublicationStatus.Error;
                                publicationRequest.StatusMessage = ex.Message;
                                await Db.SaveChangesAsync();
                                return;
                            }

                            if (Crf != null)
                            {
                                publicationRequest.LiveReadyFilesObj = publicationRequest.LiveReadyFilesObj.Append(Crf).ToList();
                                publicationRequest.UploadedRelatedFilesObj = publicationRequest.UploadedRelatedFilesObj.Where(f => f.FileUploadId != UploadedFileRef.FileUploadId).ToList();

                                if (Crf.FilePurpose.ToLower() == "mastercontent")
                                {
                                    ContentRelatedFile MasterCrf = ProcessMasterContentFile(Crf, ThisRequestGuid, publicationRequest.RootContentItem.DoesReduce, exchangePath);
                                    publicationRequest.ReductionRelatedFilesObj = publicationRequest.ReductionRelatedFilesObj.Append(new ReductionRelatedFiles { MasterContentFile = MasterCrf }).ToList();
                                }
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Publication request cannot be created for unsupported ContentType {publicationRequest.RootContentItem.ContentType.TypeEnum.ToString()}");
                }

                List<AssociatedFileModel> associatedFiles = publicationRequest.RequestedAssociatedFileList;
                foreach (AssociatedFileModel UploadedFileRef in associatedFiles)
                {
                    // move uploaded file(s) to content folder with temporary name(s)
                    ContentAssociatedFile Caf = await HandleAssociatedFile(Db, UploadedFileRef, publicationRequest.RootContentItem, publicationRequestId, contentItemRootPath);

                    if (Caf != null)
                    {
                        publicationRequest.LiveReadyAssociatedFilesList = publicationRequest.LiveReadyAssociatedFilesList.Append(Caf).ToList();
                        publicationRequest.RequestedAssociatedFileList = publicationRequest.RequestedAssociatedFileList.Where(f => f.Id != UploadedFileRef.Id).ToList();
                    }
                }

                publicationRequest.RequestStatus = PublicationStatus.Queued;

                // Update the request record with file info and Queued status
                try
                {
                    await Db.SaveChangesAsync();
                    postProcessingTaskQueue.QueuePublicationPostProcess(publicationRequest.Id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    GlobalFunctions.IssueLog(IssueLogEnum.PublishingStuck, ex, $"DbUpdateConcurrencyException encountered for publication request {publicationRequestId} while attempting to set request status to Queued");
                    // PublicationRequest was likely set to canceled, no extra cleanup needed
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Exception while queuing either publication request or postprocessing, for publication request Id <{publicationRequest.Id}>");
                    throw;
                }
            }
        }

        /// <summary>
        /// Copies an uploaded file to the proper content item folder and purges the file from uploads
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="RelatedFile"></param>
        /// <param name="ContentItem"></param>
        /// <param name="PubRequestId"></param>
        /// <param name="contentItemRootPath"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static async Task<ContentRelatedFile> HandleRelatedFile(ApplicationDbContext Db, UploadedRelatedFile RelatedFile, RootContentItem ContentItem, Guid PubRequestId, string contentItemRootPath)
        {
            ContentRelatedFile ReturnObj = null;

            using (IDbContextTransaction Txn = await Db.Database.BeginTransactionAsync())
            {
                FileUpload FileUploadRecord = await Db.FileUpload
                    .Where(f => f.Id == RelatedFile.FileUploadId)
                    .Where(f => f.Status == FileUploadStatus.Complete)
                    .SingleOrDefaultAsync();

                #region Validate the file referenced by the FileUpload record
                // The file must exist
                if (FileUploadRecord == null || !File.Exists(FileUploadRecord.StoragePath))
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, uploaded file not found at path [{FileUploadRecord.StoragePath}].");
                }
                // The checksum must be correct
                (string checksum, long length) = GlobalFunctions.GetFileChecksum(FileUploadRecord.StoragePath);
                if (!FileUploadRecord.Checksum.Equals(checksum, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, checksum {checksum} invalid for uploaded file [{FileUploadRecord.StoragePath}], length was {length}.");
                }
                #endregion

                string RootContentFolder = Path.Combine(contentItemRootPath, ContentItem.Id.ToString());

                // Copy uploaded file to root content folder
                string DestinationFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName(RelatedFile.FilePurpose, PubRequestId, ContentItem.Id, Path.GetExtension(FileUploadRecord.StoragePath));
                switch (ContentItem.ContentType.TypeEnum)
                {  // This is where any dependence on ContentType would be incorporated to override base behavior
                    case ContentTypeEnum.PowerBi:
                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.FileDownload:
                        break;
                    default:
                        break;
                }
                string DestinationFullPath = Path.Combine(RootContentFolder, DestinationFileName);

                // Create the root content folder if it does not already exist
                Directory.CreateDirectory(RootContentFolder);
                File.Copy(FileUploadRecord.StoragePath, DestinationFullPath, true);

                ReturnObj = new ContentRelatedFile
                {
                    FilePurpose = RelatedFile.FilePurpose,
                    FullPath = DestinationFullPath,
                    FileOriginalName = RelatedFile.FileOriginalName,
                    Checksum = FileUploadRecord.Checksum,
                };

                // Remove FileUpload record(s) for this file path
                List<FileUpload> Uploads = await Db.FileUpload.Where(f => f.StoragePath == FileUploadRecord.StoragePath).ToListAsync();
                File.Delete(FileUploadRecord.StoragePath);  // delete the file
                Db.FileUpload.RemoveRange(Uploads);  // remove the record

                await Db.SaveChangesAsync();
                await Txn.CommitAsync();
            }

            return ReturnObj;
        }

        private static async Task<ContentAssociatedFile> HandleAssociatedFile(ApplicationDbContext Db, AssociatedFileModel uploadedFile, RootContentItem ContentItem, Guid PubRequestId, string contentItemRootPath)
        {
            ContentAssociatedFile ReturnObj = null;

            using (IDbContextTransaction Txn = await Db.Database.BeginTransactionAsync())
            {
                FileUpload fileUploadRecord = await Db.FileUpload
                                                      .Where(f => f.Id == uploadedFile.Id)
                                                      .Where(f => f.Status == FileUploadStatus.Complete)
                                                      .SingleOrDefaultAsync();

                string RootContentFolder = Path.Combine(contentItemRootPath, ContentItem.Id.ToString());
                string DestinationFileName = ContentTypeSpecificApiBase.GeneratePreliveAssociatedFileName(uploadedFile.Id, PubRequestId, ContentItem.Id, Path.GetExtension(fileUploadRecord.StoragePath));
                string DestinationFullPath = Path.Combine(RootContentFolder, DestinationFileName);

                // Create the root content folder if it does not already exist
                Directory.CreateDirectory(RootContentFolder);
                File.Copy(fileUploadRecord.StoragePath, DestinationFullPath, true);

                ReturnObj = new ContentAssociatedFile
                {
                    Id = uploadedFile.Id,
                    Checksum = fileUploadRecord.Checksum,
                    DisplayName = uploadedFile.DisplayName,
                    FileOriginalName = uploadedFile.FileOriginalName,
                    FileType = uploadedFile.FileType,
                    SortOrder = uploadedFile.SortOrder,
                    FullPath = DestinationFullPath,
                };

                // Remove FileUpload record(s) for this file path
                List<FileUpload> Uploads = await Db.FileUpload.Where(f => f.StoragePath == fileUploadRecord.StoragePath).ToListAsync();
                File.Delete(fileUploadRecord.StoragePath);  // delete the uploaded file
                Db.FileUpload.RemoveRange(Uploads);  // remove the FileUpload record

                await Db.SaveChangesAsync();
                await Txn.CommitAsync();
            }

            return ReturnObj;
        }

        /// <summary>
        /// Copies a master content file to the FileExchange share
        /// </summary>
        /// <param name="FileDetails"></param>
        /// <param name="RequestGuid"></param>
        /// <param name="DoesReduce"></param>
        /// <param name="exchangePath"></param>
        /// <returns></returns>
        private static ContentRelatedFile ProcessMasterContentFile(ContentRelatedFile FileDetails, Guid RequestGuid, bool DoesReduce, string exchangePath)
        {
            if (DoesReduce)
            {
                string MapPublishingServerExchangeRequestFolder = Path.Combine(exchangePath, RequestGuid.ToString("D"));
                Directory.CreateDirectory(MapPublishingServerExchangeRequestFolder);
                string DestinationFullPath = Path.Combine(MapPublishingServerExchangeRequestFolder, Path.GetFileName(FileDetails.FullPath));
                File.Copy(FileDetails.FullPath, DestinationFullPath, true);

                return new ContentRelatedFile
                {
                    FullPath = DestinationFullPath,
                    FilePurpose = FileDetails.FilePurpose,
                    Checksum = FileDetails.Checksum,
                    FileOriginalName = FileDetails.FileOriginalName,
                };
            }
            else
            {
                return FileDetails;
            }
        }
    }
}
