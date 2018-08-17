/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Functionality to support actions in the ContentAccessAdminController
 * DEVELOPER NOTES: Strongly consider utilizing a background task queue for this in the future.
 *      See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapCommonLib;
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

        internal static void MonitorPublicationRequestForQueueing(long publicationRequestId, UploadedRelatedFile[] files, string connectionString, string contentItemRootPath, string exchangePath)
        {
            bool validationWindowComplete = false;

            DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
            DbContextOptions<ApplicationDbContext> ContextOptions = ContextBuilder.Options;

            while (!validationWindowComplete)
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    var fileIds = files.Select(f => f.FileUploadId).ToList();
                    validationWindowComplete = Db.FileUpload
                        .Where(u => fileIds.Contains(u.Id))
                        .All(u => u.VirusScanWindowComplete);
                }

                Thread.Sleep(1000);
            }

            Guid ThisRequestGuid = Guid.NewGuid();

            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                var publicationRequest = Db.ContentPublicationRequest.Single(r => r.Id == publicationRequestId);
                var rootContentItem = Db.RootContentItem
                    .Where(i => i.Id == publicationRequest.RootContentItemId)
                    .Include(i => i.ContentType)
                    .Single();
                switch (rootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        // TODO move this logic to a class in project QlikviewLib, derived from Interface or base class in MapCommonLib
                        if (files.Select(f => f.FilePurpose).Count(p => p.ToLower() == "mastercontent") > 1)
                        {
                            throw new ApplicationException("Qlikview publication request cannot contain multiple MasterContent files");
                        }

                        foreach (UploadedRelatedFile UploadedFileRef in files)
                        {
                            ContentRelatedFile Crf = HandleRelatedFile(Db, UploadedFileRef, rootContentItem, publicationRequestId, contentItemRootPath);

                            if (Crf != null)
                            {
                                publicationRequest.LiveReadyFilesObj = publicationRequest.LiveReadyFilesObj.Append(Crf).ToList();

                                if (Crf.FilePurpose.ToLower() == "mastercontent" && rootContentItem.DoesReduce)
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
                Db.SaveChanges();

                AuditLogger Logger = new AuditLogger();
                Logger.Log(AuditEventType.PublicationQueued.ToEvent(rootContentItem, publicationRequest));
            }
        }

        private static ContentRelatedFile HandleRelatedFile(ApplicationDbContext Db, UploadedRelatedFile RelatedFile, RootContentItem ContentItem, long PubRequestId, string contentItemRootPath)
        {
            ContentRelatedFile ReturnObj = null;

            using (IDbContextTransaction Txn = Db.Database.BeginTransaction())
            {
                FileUpload FileUploadRecord = Db.FileUpload.Find(RelatedFile.FileUploadId);

                #region Validate the file referenced by the FileUpload record
                if (!System.IO.File.Exists(FileUploadRecord.StoragePath))
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, uploaded file not found at path [{FileUploadRecord.StoragePath}].");
                }
                if (FileUploadRecord.Checksum.ToLower() != GlobalFunctions.GetFileChecksum(FileUploadRecord.StoragePath).ToLower())
                {
                    throw new ApplicationException($"While publishing for content {ContentItem.Id}, checksum validation failed for file [{FileUploadRecord.StoragePath}].");
                }
                #endregion

                string RootContentFolder = Path.Combine(contentItemRootPath, ContentItem.Id.ToString());

                // Copy uploaded file to root content folder
                string DestinationFileName = $"{RelatedFile.FilePurpose}.Pub[{PubRequestId.ToString()}].Content[{ContentItem.Id.ToString()}]{Path.GetExtension(FileUploadRecord.StoragePath)}";
                string DestinationFullPath = Path.Combine(RootContentFolder, DestinationFileName);

                // Create the root content folder if it does not already exist
                Directory.CreateDirectory(RootContentFolder);
                System.IO.File.Copy(FileUploadRecord.StoragePath, DestinationFullPath, true);

                ReturnObj = new ContentRelatedFile
                {
                    FilePurpose = RelatedFile.FilePurpose,
                    FullPath = DestinationFullPath,
                    FileOriginalName = RelatedFile.FileOriginalName,
                    Checksum = FileUploadRecord.Checksum,
                };

                // Remove FileUpload record(s) for this file path
                List<FileUpload> Uploads = Db.FileUpload.Where(f => f.StoragePath == FileUploadRecord.StoragePath).ToList();
                System.IO.File.Delete(FileUploadRecord.StoragePath);
                Db.FileUpload.RemoveRange(Uploads);
                //Uploads.ForEach(u => Db.FileUpload.r.Remove(u));

                Db.SaveChanges();
                Txn.Commit();
            }

            return ReturnObj;
        }

        private static ContentRelatedFile ProcessMasterContentFile(ContentRelatedFile FileDetails, Guid RequestGuid, bool DoesReduce, string exchangePath)
        {
            if (DoesReduce)
            {
                string MapPublishingServerExchangeRequestFolder = Path.Combine(exchangePath, RequestGuid.ToString("D"));
                Directory.CreateDirectory(MapPublishingServerExchangeRequestFolder);
                string DestinationFullPath = Path.Combine(MapPublishingServerExchangeRequestFolder, Path.GetFileName(FileDetails.FullPath));
                System.IO.File.Copy(FileDetails.FullPath, DestinationFullPath, true);

                return new ContentRelatedFile
                {
                    FullPath = DestinationFullPath,
                    FilePurpose = FileDetails.FilePurpose,
                    Checksum = FileDetails.Checksum,
                };
            }

            return null;
        }
    }
}
