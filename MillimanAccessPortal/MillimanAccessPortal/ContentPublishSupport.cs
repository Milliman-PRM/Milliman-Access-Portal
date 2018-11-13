/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Functionality to support actions in the ContentPublishingController
 * DEVELOPER NOTES: Strongly consider utilizing a background task queue for this in the future.
 *      See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapCommonLib;
using QlikviewLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MillimanAccessPortal.Models.ContentPublishing;
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

        internal static void MonitorPublicationRequestForQueueing(Guid publicationRequestId, string connectionString, string contentItemRootPath, string exchangePath)
        {
            bool validationWindowComplete = false;

            DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
            DbContextOptions<ApplicationDbContext> ContextOptions = ContextBuilder.Options;

            List<Guid> fileIds;
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                var publicationRequest = Db.ContentPublicationRequest.Single(r => r.Id == publicationRequestId);
                fileIds = publicationRequest.UploadedRelatedFilesObj.Select(f => f.FileUploadId).ToList();
            }
            while (!validationWindowComplete)
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    validationWindowComplete = Db.FileUpload
                        .Where(u => fileIds.Contains(u.Id))
                        .All(u => u.VirusScanWindowComplete);
                }

                Thread.Sleep(1000);
            }
            // Validation of all uploads is complete

            Guid ThisRequestGuid = Guid.NewGuid();

            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                var publicationRequest = Db.ContentPublicationRequest.SingleOrDefault(r => r.Id == publicationRequestId);
                var publicationStatus = publicationRequest?.RequestStatus ?? PublicationStatus.Canceled;
                if (publicationStatus == PublicationStatus.Canceled)
                {
                    return;
                }

                var files = publicationRequest.UploadedRelatedFilesObj;
                var rootContentItem = Db.RootContentItem
                    .Where(i => i.Id == publicationRequest.RootContentItemId)
                    .Include(i => i.ContentType)
                    .Single();
                switch (rootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.FileDownload:
                        // Only one mastercontent file is supported with this content type
                        if (files.Select(f => f.FilePurpose).Count(p => p.ToLower() == "mastercontent") > 1)
                        {
                            throw new ApplicationException("Qlikview publication request cannot contain multiple MasterContent files");
                        }

                        foreach (UploadedRelatedFile UploadedFileRef in files)
                        {
                            // move uploaded file(s) to content folder with temporary name(s)
                            ContentRelatedFile Crf = HandleRelatedFile(Db, UploadedFileRef, rootContentItem, publicationRequestId, contentItemRootPath, rootContentItem.ContentType.TypeEnum);

                            if (Crf != null)
                            {
                                publicationRequest.LiveReadyFilesObj = publicationRequest.LiveReadyFilesObj.Append(Crf).ToList();
                                publicationRequest.UploadedRelatedFilesObj = publicationRequest.UploadedRelatedFilesObj.Where(f => f.FileUploadId != UploadedFileRef.FileUploadId).ToList();

                                if (Crf.FilePurpose.ToLower() == "mastercontent")
                                {
                                    ContentRelatedFile MasterCrf = ProcessMasterContentFile(Crf, ThisRequestGuid, rootContentItem.DoesReduce, exchangePath);
                                    publicationRequest.ReductionRelatedFilesObj = publicationRequest.ReductionRelatedFilesObj.Append(new ReductionRelatedFiles { MasterContentFile = MasterCrf }).ToList();
                                }
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Publication request cannot be created for unsupported ContentType {rootContentItem.ContentType.TypeEnum.ToString()}");
                }

                publicationRequest.RequestStatus = PublicationStatus.Queued;

                // Update the request record with file info and Queued status
                Db.ContentPublicationRequest.Update(publicationRequest);
                try
                {
                    Db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // PublicationRequest was set to canceled, no extra cleanup needed
                    return;
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
        private static ContentRelatedFile HandleRelatedFile(ApplicationDbContext Db, UploadedRelatedFile RelatedFile, RootContentItem ContentItem, Guid PubRequestId, string contentItemRootPath, ContentTypeEnum contentType)
        {
            ContentRelatedFile ReturnObj = null;

            using (IDbContextTransaction Txn = Db.Database.BeginTransaction())
            {
                FileUpload FileUploadRecord = Db.FileUpload.Find(RelatedFile.FileUploadId);

                #region Validate the file referenced by the FileUpload record
                // The file must exist
                if (FileUploadRecord == null || !System.IO.File.Exists(FileUploadRecord.StoragePath))
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, uploaded file not found at path [{FileUploadRecord.StoragePath}].");
                }
                // The checksum must be correct
                if (FileUploadRecord.Checksum.ToLower() != GlobalFunctions.GetFileChecksum(FileUploadRecord.StoragePath).ToLower())
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, checksum validation failed for file [{FileUploadRecord.StoragePath}].");
                }
                #endregion

                string RootContentFolder = Path.Combine(contentItemRootPath, ContentItem.Id.ToString());

                // Copy uploaded file to root content folder
                string DestinationFileName = QlikviewLibApi.GeneratePreliveRelatedFileName(RelatedFile.FilePurpose, PubRequestId, ContentItem.Id, Path.GetExtension(FileUploadRecord.StoragePath));
                switch (contentType)
                {  // This is where any dependence on ContentType would be incorporated to override base behavior
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
                List<FileUpload> Uploads = Db.FileUpload.Where(f => f.StoragePath == FileUploadRecord.StoragePath).ToList();
                File.Delete(FileUploadRecord.StoragePath);  // delete the file
                Db.FileUpload.RemoveRange(Uploads);  // remove the record

                Db.SaveChanges();
                Txn.Commit();
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
