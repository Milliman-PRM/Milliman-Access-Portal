using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Services;
using QlikviewLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class QueuedGoLiveTaskHostedService : BackgroundService
{
    public QueuedGoLiveTaskHostedService(
        IServiceProvider services,
        IGoLiveTaskQueue taskQueue)
    {
        Services = services;
        TaskQueue = taskQueue;
    }

    public IServiceProvider Services { get; }
    public IGoLiveTaskQueue TaskQueue { get; }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using (var scope = Services.CreateScope())
        {
            var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var qlikviewConfig = scope.ServiceProvider.GetRequiredService<IOptions<QlikviewConfig>>().Value;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve the relevant data to finalize the goLive
                var goLiveViewModel = await TaskQueue.DequeueAsync(cancellationToken);

                await ProcessGoLive(
                    goLiveViewModel,
                    dbContext,
                    auditLogger,
                    configuration,
                    qlikviewConfig);
            }
        }
    }

    private async Task ProcessGoLive(
        GoLiveViewModel goLiveViewModel, ApplicationDbContext dbContext, IAuditLogger auditLogger,
        IConfiguration configuration, QlikviewConfig qlikviewConfig)
    {
        #region Checksum verification
        var publicationRequest = dbContext.ContentPublicationRequest
            .Include(r => r.RootContentItem)
                .ThenInclude(c => c.ContentType)
            .Include(r => r.ApplicationUser)
            .Where(r => r.Id == goLiveViewModel.PublicationRequestId)
            .Where(r => r.RootContentItemId == goLiveViewModel.RootContentItemId)
            .SingleOrDefault(r => r.RequestStatus == PublicationStatus.Confirming);

        if (publicationRequest?.RootContentItem == null || publicationRequest?.ApplicationUser == null)
        {
            Log.Error(
                "In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                $"publication request {goLiveViewModel.PublicationRequestId} not found, " + 
                $"or related user {publicationRequest?.ApplicationUserId} not found, " +
                $"or related content item {publicationRequest?.RootContentItemId} not found");
            return;
        }

        bool ReductionIsInvolved = publicationRequest.RootContentItem.DoesReduce
            && publicationRequest.LiveReadyFilesObj.Any(f => f.FilePurpose.ToLower() == "mastercontent");

        var relatedReductionTasks = dbContext.ContentReductionTask
            .Include(t => t.SelectionGroup)
                .ThenInclude(g => g.RootContentItem)
                    .ThenInclude(c => c.ContentType)
            .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
            .ToList();

        if (ReductionIsInvolved)
        {
            // For each reducing SelectionGroup related to the RootContentItem:
            var relatedSelectionGroups = dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == goLiveViewModel.RootContentItemId)
                .Where(g => !g.IsMaster);
            foreach (var relatedSelectionGroup in relatedSelectionGroups)
            {
                ContentReductionTask ThisTask;

                // RelatedReductionTasks should have one ContentReductionTask related to the SelectionGroup
                try
                {
                    ThisTask = relatedReductionTasks.Single(t => t.SelectionGroupId == relatedSelectionGroup.Id);
                }
                catch (InvalidOperationException)
                {
                    Log.Error($"" +
                        "In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                        "expected one reduction task for each non-master selection group, " +
                        $"failed for selection group {relatedSelectionGroup.Id}, " +
                        "aborting");
                    await FailGoLive(dbContext, publicationRequest,
                        $"Expected 1 reduction task related to SelectionGroup {relatedSelectionGroup.Id}, " +
                        "cannot complete this go-live request.");
                    return;
                }

                // Validate file checksum for reduced content
                var currentChecksum = GlobalFunctions.GetFileChecksum(ThisTask.ResultFilePath).ToLower();
                if (currentChecksum != ThisTask.ReducedContentChecksum.ToLower())
                {
                    Log.Error($"In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                        "for selection group {relatedSelectionGroup.Id}, " +
                        "reduced content file {ThisTask.ResultFilePath} failed checksum validation, " +
                        "aborting");
                    auditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
                        publicationRequest.RootContentItem, publicationRequest));
                    await FailGoLive(dbContext, publicationRequest,
                        $"Reduced content file failed integrity check, " +
                        "cannot complete the go-live request.");
                    return;
                }
            }
        }

        // Validate Checksums of LiveReady files
        foreach (var Crf in publicationRequest.LiveReadyFilesObj)
        {
            if (!Crf.ValidateChecksum())
            {
                Log.Error($"In QueueGoLiveTaskHostedService.ExecuteAsync action: " +
                    $"for publication request {publicationRequest.Id}, " +
                    $"live ready file {Crf.FullPath} failed checksum validation, " +
                    "aborting");
                auditLogger.Log(AuditEventType.GoLiveValidationFailed.ToEvent(
                    publicationRequest.RootContentItem, publicationRequest));
                await FailGoLive(dbContext, publicationRequest, "File integrity validation failed");
                return;
            }
        }
        #endregion

        #region Go live
        List<string> FilesToDelete = new List<string>();

        using (IDbContextTransaction Txn = dbContext.Database.BeginTransaction())
        {
            // 1 Move new master content and related files (not reduced content) into live file names,
            //   removing any existing copies of previous version
            List<ContentRelatedFile> UpdatedContentFilesList = publicationRequest.RootContentItem.ContentFilesList;
            foreach (ContentRelatedFile Crf in publicationRequest.LiveReadyFilesObj)
            {
                // This assignment defines the live file name
                string TargetFileName = ContentTypeSpecificApiBase.GenerateContentFileName(
                    Crf.FilePurpose, Path.GetExtension(Crf.FullPath), goLiveViewModel.RootContentItemId);
                string TargetFilePath = Path.Combine(Path.GetDirectoryName(Crf.FullPath), TargetFileName);

                // Move any existing file to backed up name
                if (File.Exists(TargetFilePath))
                {
                    string BackupFilePath = TargetFilePath + ".bak";
                    if (File.Exists(BackupFilePath))
                    {
                        File.Delete(BackupFilePath);
                    }
                    File.Move(TargetFilePath, BackupFilePath);
                    FilesToDelete.Add(BackupFilePath);
                }

                // Deallocate any temporary document licenses for preview file(s)
                switch (publicationRequest.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        var qvwFiles = publicationRequest.LiveReadyFilesObj
                            .Where(f => string.Compare(Path.GetExtension(f.FullPath), ".qvw", true) == 0);
                        foreach (ContentRelatedFile Qvw in qvwFiles)
                        {
                            string FileRelativePath = Path.GetRelativePath(configuration
                                .GetValue<string>("Storage:ContentItemRootPath"), Qvw.FullPath);
                            await new QlikviewLibApi().ReclaimAllDocCalsForFile(FileRelativePath, qlikviewConfig);
                        }
                        break;

                    default:
                        break;
                }

                // Can't move between different volumes
                File.Copy(Crf.FullPath, TargetFilePath);
                FilesToDelete.Add(Crf.FullPath);

                UpdatedContentFilesList.RemoveAll(f => f.FilePurpose.ToLower() == Crf.FilePurpose.ToLower());
                UpdatedContentFilesList.Add(new ContentRelatedFile
                {
                    FilePurpose = Crf.FilePurpose,
                    FullPath = TargetFilePath,
                    Checksum = Crf.Checksum,
                    FileOriginalName = Crf.FileOriginalName,
                });

                // Set content URL in each master SelectionGroup
                if (Crf.FilePurpose.ToLower() == "mastercontent")
                {
                    IEnumerable<SelectionGroup> MasterSelectionGroupQuery = null;
                    if (publicationRequest.RootContentItem.DoesReduce)
                    {
                        MasterSelectionGroupQuery = relatedReductionTasks
                            .Select(t => t.SelectionGroup)
                            .Where(g => g.IsMaster);
                    }
                    else
                    {
                        MasterSelectionGroupQuery = dbContext.SelectionGroup
                            .Where(g => g.RootContentItemId == publicationRequest.RootContentItemId)
                            .Where(g => g.IsMaster);
                    }
                    foreach (SelectionGroup MasterContentGroup in MasterSelectionGroupQuery)
                    {
                        MasterContentGroup.SetContentUrl(TargetFileName);
                        dbContext.SelectionGroup.Update(MasterContentGroup);
                    }
                }
            }
            publicationRequest.RootContentItem.ContentFilesList = UpdatedContentFilesList;

            // 2 Rename reduced content files to live names
            foreach (var ThisTask in relatedReductionTasks.Where(t => !t.SelectionGroup.IsMaster))
            {
                // This assignment defines the live file name for any reduced content file
                string TargetFileName = ContentTypeSpecificApiBase.GenerateReducedContentFileName(
                    ThisTask.SelectionGroupId,
                    publicationRequest.RootContentItemId,
                    Path.GetExtension(ThisTask.ResultFilePath));
                string TargetFilePath = Path.Combine(
                    configuration.GetSection("Storage")["ContentItemRootPath"],
                    publicationRequest.RootContentItemId.ToString(),
                    TargetFileName);

                // Set url in SelectionGroup
                ThisTask.SelectionGroup.SetContentUrl(TargetFileName);
                ThisTask.SelectionGroup.ReducedContentChecksum = ThisTask.ReducedContentChecksum;
                dbContext.SelectionGroup.Update(ThisTask.SelectionGroup);

                // Move the existing file to backed up name if exists
                if (File.Exists(TargetFilePath))
                {
                    string BackupFilePath = TargetFilePath + ".bak";
                    if (File.Exists(BackupFilePath))
                    {
                        File.Delete(BackupFilePath);
                    }
                    File.Move(TargetFilePath, BackupFilePath);
                    FilesToDelete.Add(BackupFilePath);
                }

                File.Copy(ThisTask.ResultFilePath, TargetFilePath);
                FilesToDelete.Add(ThisTask.ResultFilePath);
            }

            //3 Update db:
            //3.1  ContentPublicationRequest.Status
            var previousLiveRequests = dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == publicationRequest.RootContentItemId)
                .Where(r => r.RequestStatus == PublicationStatus.Confirmed);
            foreach (ContentPublicationRequest PreviousLiveRequest in previousLiveRequests)
            {
                PreviousLiveRequest.RequestStatus = PublicationStatus.Replaced;
                dbContext.ContentPublicationRequest.Update(PreviousLiveRequest);
            }
            publicationRequest.RequestStatus = PublicationStatus.Confirmed;

            //3.2  ContentReductionTask.Status
            var previousLiveTasks = dbContext.ContentReductionTask
                .Where(r => r.SelectionGroup.RootContentItemId == publicationRequest.RootContentItemId)
                .Where(r => r.ReductionStatus == ReductionStatusEnum.Live);
            foreach (ContentReductionTask PreviousLiveTask in previousLiveTasks)
            {
                PreviousLiveTask.ReductionStatus = ReductionStatusEnum.Replaced;
                dbContext.ContentReductionTask.Update(PreviousLiveTask);
            }
            relatedReductionTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Live);

            var LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>
                .GetHierarchyForRootContentItem(dbContext, publicationRequest.RootContentItemId);
            var NewHierarchy = relatedReductionTasks[0].MasterContentHierarchyObj;

            //3.3  HierarchyFieldValue due to hierarchy changes
            //3.3.1  If this is first publication for this root content item, add the fields to db
            //       and to LiveHierarchy to help identify all values as new
            if (LiveHierarchy.Fields.Count == 0)
            {  // This must be first time publication, need to insert the fields.  Values are handled below
                NewHierarchy.Fields.ForEach(f =>
                {
                    HierarchyField NewField = new HierarchyField
                    {
                        FieldName = f.FieldName,
                        FieldDisplayName = f.DisplayName,
                        RootContentItemId = publicationRequest.RootContentItemId,
                        FieldDelimiter = f.ValueDelimiter,
                        StructureType = f.StructureType,
                    };
                    dbContext.HierarchyField.Add(NewField);
                    dbContext.SaveChanges();

                    LiveHierarchy.Fields.Add(new ReductionField<ReductionFieldValue>
                    {
                        Id = NewField.Id,  // Id is assigned during dbContext.SaveChanges() above
                        FieldName = NewField.FieldName,
                        DisplayName = NewField.FieldDisplayName,
                        StructureType = NewField.StructureType,
                        ValueDelimiter = NewField.FieldDelimiter,
                        Values = new List<ReductionFieldValue>(),
                    });
                });
            }
            //3.3.2  Add/Remove field values based on value list differences between new/old
            foreach (var NewHierarchyField in NewHierarchy.Fields)
            {
                ReductionField<ReductionFieldValue> MatchingLiveField = LiveHierarchy.Fields
                    .Single(f => f.FieldName == NewHierarchyField.FieldName);

                List<string> NewHierarchyFieldValueList = NewHierarchyField.Values.Select(v => v.Value).ToList();
                List<string> LiveHierarchyFieldValueList = MatchingLiveField.Values.Select(v => v.Value).ToList();

                // Insert new values
                foreach (string NewValue in NewHierarchyFieldValueList.Except(LiveHierarchyFieldValueList))
                {
                    dbContext.HierarchyFieldValue.Add(new HierarchyFieldValue
                    {
                        HierarchyFieldId = MatchingLiveField.Id,
                        Value = NewValue,
                    });
                }

                // Delete removed values
                foreach (string RemovedValue in LiveHierarchyFieldValueList.Except(NewHierarchyFieldValueList))
                {
                    HierarchyFieldValue ObsoleteRecord = dbContext.HierarchyFieldValue
                        .Single(v =>
                            v.HierarchyField.RootContentItemId == publicationRequest.RootContentItemId
                            && v.Value == RemovedValue);
                    dbContext.HierarchyFieldValue.Remove(ObsoleteRecord);
                }
            }
            dbContext.SaveChanges();

            //3.4  Update SelectionGroup SelectedHierarchyFieldValueList due to hierarchy changes
            List<Guid> AllRemainingFieldValues = dbContext.HierarchyFieldValue
                .Where(v => v.HierarchyField.RootContentItemId == publicationRequest.RootContentItemId)
                .Select(v => v.Id)
                .ToList();
            var reducingSelectionGroups = dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == publicationRequest.RootContentItemId && !g.IsMaster);
            foreach (SelectionGroup Group in reducingSelectionGroups)
            {
                Group.SelectedHierarchyFieldValueList = Group.SelectedHierarchyFieldValueList
                    .Intersect(AllRemainingFieldValues).ToArray();
            }

            // Perform any content type dependent follow up processing
            switch (publicationRequest.RootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                    await new QlikviewLibApi()
                        .AuthorizeUserDocumentsInFolder(
                            goLiveViewModel.RootContentItemId.ToString(), qlikviewConfig);
                    break;

                case ContentTypeEnum.Unknown:
                default:
                    break;
            }

            dbContext.SaveChanges();
            Txn.Commit();
        }

        Log.Verbose(
            "In ContentPublishingController.GoLive action: " +
            $"publication request {publicationRequest.Id} success");
        auditLogger.Log(AuditEventType.ContentPublicationGoLive.ToEvent(
            publicationRequest.RootContentItem, publicationRequest, goLiveViewModel.ValidationSummaryId));

        // 4 Delete all temporary files
        foreach (string FileToDelete in FilesToDelete)
        {
            try
            {
                File.Delete(FileToDelete);
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
        }

        if (ReductionIsInvolved)
        {
            //Delete temporary folder of publication job (contains temporary reduced content files)
            string PubJobTempFolder = Path.GetDirectoryName(relatedReductionTasks[0].MasterFilePath);
            Directory.Delete(PubJobTempFolder, true);
        }
        #endregion
    }

    private async Task FailGoLive(
        ApplicationDbContext dbContext, ContentPublicationRequest publicationRequest, string reason)
    {
        publicationRequest.RequestStatus = PublicationStatus.Error;
        publicationRequest.StatusMessage = reason;
        await dbContext.SaveChangesAsync();
    }
} 
