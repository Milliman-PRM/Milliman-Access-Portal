/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AuditLogLib;
using AuditLogLib.Event;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Newtonsoft.Json;

namespace ContentPublishingLib.JobRunners
{
    public class MapDbPublishRunner : RunnerBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        string _ContentItemRootPath = string.Empty;

        public PublishJobDetail JobDetail { get; set; } = new PublishJobDetail();

        /// <summary>
        /// Initializes data used to construct database context instances using a named configuration parameter.
        /// </summary>
        internal string ConfiguredConnectionStringParamName
        {
            set
            {
                ConnectionString = Configuration.ApplicationConfiguration.GetConnectionString(value);
            }
        }

        /// <summary>
        /// Initializes data used to construct database context instances.
        /// </summary>
        public string ConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                ContextOptions = ContextBuilder.Options;
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        public MapDbPublishRunner()
        {
            _ContentItemRootPath = Configuration.ApplicationConfiguration.GetSection("Storage")["ContentItemRootPath"];
            if (!Directory.Exists(_ContentItemRootPath))
            {
                throw new ApplicationException($"Configured Storage:ContentItemRootPath folder <{_ContentItemRootPath}> does not exist");
            }
        }

        /// <summary>
        /// Main functional entry point for the runner, 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PublishJobDetail> Execute(CancellationToken cancellationToken)
        {
            long limitTicksPerReductionTask = new TimeSpan(0, 10, 0).Ticks;

            JobDetail.Result.StartDateTime = DateTime.UtcNow;
            DateTime StartUtc = DateTime.UtcNow;

            _CancellationToken = cancellationToken;
            // AuditLog would not be null during a test run where it may be initialized earlier
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            try
            {
                switch (JobDetail.Request.ContentType)
                {
                    case ContentTypeEnum.Qlikview:
                        if (JobDetail.Request.MasterContentFile != null && !JobDetail.Request.SkipReductionTaskQueueing)
                        {
                            await QueueReductionActivityAsync(JobDetail.Request.MasterContentFile);
                        }
                        break;

                    case ContentTypeEnum.PowerBi:
                        if (JobDetail.Request.DoesReduce)
                        {
                            await GeneratePbiRlsReductionTaskRecords();
                        }
                        break;
                }

                int PendingTaskCount = await CountPendingReductionTasks();
                TimeSpan timeLimit = TimeSpan.FromTicks(limitTicksPerReductionTask * PendingTaskCount);

                // Wait for any/all related reduction tasks to complete
                for ( ; PendingTaskCount > 0; PendingTaskCount = await CountPendingReductionTasks())
                {
                    using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                    {
                        var cancelableTasks = await Db.ContentReductionTask
                                                      .Include(t => t.SelectionGroup)
                                                      .Where(t => t.ContentPublicationRequestId == JobDetail.JobId)
                                                      .Where(t => ReductionStatusExtensions.cancelableStatusList.Contains(t.ReductionStatus))
                                                      .ToListAsync();

                        if (DateTime.UtcNow > StartUtc + timeLimit)
                        {
                            await CancelReductionTasks(cancelableTasks.Select(t => t.Id), $"Processing of the publication was canceled because the time limit of {timeLimit} was exceeded.");

                            // throw so that the exception message gets recorded in the ContentPublicationRequest.StatusMessage field
                            throw new ApplicationException($"Publication {JobDetail.JobId} timed out waiting for {PendingTaskCount} pending reduction tasks");
                        }

                        var FailedTasks = await Db.ContentReductionTask
                                                  .Where(t => t.ContentPublicationRequestId == JobDetail.JobId
                                                           && t.ReductionStatus == ReductionStatusEnum.Error)
                                                  .ToListAsync();
                        if (FailedTasks.Any())
                        {
                            await CancelReductionTasks(cancelableTasks.Select(t => t.Id), "This reduction was canceled due to an unrecoverable error in another reduction of the same publication");

                            // throw so that the exception message gets recorded in the ContentPublicationRequest.StatusMessage field
                            throw new ApplicationException($"Terminating publication {JobDetail.JobId} due to reduction task(s) with Error status: {string.Join(", ", FailedTasks.Select(t => t.Id.ToString()))}");
                        }
                    }

                    Thread.Sleep(1000);
                }

                List<ContentReductionTask> AllRelatedReductionTasks = null;
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    AllRelatedReductionTasks = await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToListAsync();
                }

                var unhandleableErrors = AllRelatedReductionTasks
                    .Where(t => t.ReductionStatus == ReductionStatusEnum.Error)
                    .Where(t => t.OutcomeMetadataObj.OutcomeReason.PreventsPublication());
                if (unhandleableErrors.Any())
                {
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                }
                else if (AllRelatedReductionTasks.Any() &&
                         AllRelatedReductionTasks.All(t => t.ReductionStatus == ReductionStatusEnum.Canceled))
                {
                    // If a publication timeout happens queued tasks are canceled but we won't get here because that also throws ApplicationException
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;

                    #region Log audit event
                    var DetailObj = new
                    {
                        PublicationRequestId = JobDetail.JobId,
                        JobDetail.Request.DoesReduce,
                    };
                    AuditLog.Log(AuditEventType.ContentPublicationRequestCanceled.ToEvent(DetailObj), null, null);
                    #endregion
                }
                else
                {
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;

                    foreach (var ReducingTask in AllRelatedReductionTasks.Where(t => (t.TaskAction == TaskActionEnum.ReductionOnly || t.TaskAction == TaskActionEnum.HierarchyAndReduction)
                                                                                   && t.OutcomeMetadataObj.OutcomeReason == MapDbReductionTaskOutcomeReason.Success))
                    {
                        JobDetail.Result.ResultingRelatedFiles.Add(
                            new ContentRelatedFile
                            {
                                Checksum = ReducingTask.ReducedContentChecksum,
                                FilePurpose = "ReducedContent",
                                FullPath = ReducingTask.ResultFilePath,
                                FileOriginalName = Path.GetFileName(ReducingTask.ResultFilePath),
                            }
                         );
                    }

                    #region Log audit event
                    var DetailObj = new
                    {
                        PublicationRequestId = JobDetail.JobId,
                        JobDetail.Request.DoesReduce,
                        RequestingUser = JobDetail.Request.ApplicationUserId,
                        ReductionTasks = AllRelatedReductionTasks.Select(t => t.Id.ToString("D")).ToList(),
                    };
                    AuditLog.Log(AuditEventType.PublicationRequestProcessingSuccess.ToEvent(DetailObj), null, null);
                    #endregion
                }
            }
            catch (OperationCanceledException e)
            {
                string msg = GlobalFunctions.LoggableExceptionString(e);
                Log.Error(e, $"Operation canceled in MapDbPublishRunner");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;
                JobDetail.Result.StatusMessage = msg;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Exception from MapDbPublishRunner.Execute()");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                JobDetail.Result.StatusMessage = GlobalFunctions.LoggableExceptionString(e);
            }
            finally
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    foreach (ContentReductionTask RelatedTask in await Db.ContentReductionTask
                                                                         .Where(t => t.ContentPublicationRequestId == JobDetail.JobId)
                                                                         .ToListAsync())
                    {
                        ReductionTaskOutcomeMetadata TaskOutcome = RelatedTask.OutcomeMetadataObj;
                        if (TaskOutcome.ReductionTaskId == Guid.Empty)
                        {
                            TaskOutcome.ReductionTaskId = RelatedTask.Id;
                        }

                        List<MapDbReductionTaskOutcomeReason> successListReductionReasons = new List<MapDbReductionTaskOutcomeReason>
                        {
                            MapDbReductionTaskOutcomeReason.Success,
                            MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned,
                            MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent, // warning
                            MapDbReductionTaskOutcomeReason.NoSelectedFieldValues, // warning
                            MapDbReductionTaskOutcomeReason.NoReducedFileCreated, // warning
                        };

                        if (successListReductionReasons.Contains(TaskOutcome.OutcomeReason))
                        {
                            JobDetail.Result.ReductionTaskSuccessList.Add(TaskOutcome);
                        }
                        else
                        {
                            JobDetail.Result.ReductionTaskFailList.Add(TaskOutcome);
                        }
                        Log.Debug($"MapDbPublishRunner.Execute(), recording OutcomeMetadata of related reduction task {RelatedTask.Id}");
                    }
                }

                JobDetail.Result.ReductionTaskSuccessList = JobDetail.Result.ReductionTaskSuccessList.OrderBy(t => t.ProcessingStartedUtc).ToList();
                JobDetail.Result.ReductionTaskFailList = JobDetail.Result.ReductionTaskFailList.OrderBy(t => t.ProcessingStartedUtc).ToList();
                JobDetail.Result.ElapsedTime = DateTime.UtcNow - JobDetail.Result.StartDateTime;
            }

            return JobDetail;
        }

        private async Task<int> CountPendingReductionTasks()
        {
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                PublicationStatus RequestStatus = await Db.ContentPublicationRequest
                                                          .Where(r => r.Id == JobDetail.JobId)
                                                          .Select(r => r.RequestStatus)
                                                          .FirstOrDefaultAsync();  // default is PublicationStatus.Unknown
                List<ContentReductionTask> AllRelatedReductionTasks = await Db.ContentReductionTask
                                                                              .Where(t => t.ContentPublicationRequestId == JobDetail.JobId)
                                                                              .ToListAsync();

                if (_CancellationToken.IsCancellationRequested || RequestStatus == PublicationStatus.Canceled)
                {
                    await CancelReductionTasks(AllRelatedReductionTasks.Select(t => t.Id), "This reduction was canceled because the overall publication was canceled");
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;
                    _CancellationToken.ThrowIfCancellationRequested();
                }

                return AllRelatedReductionTasks.Count(t => t.ReductionStatus == ReductionStatusEnum.Queued
                                                        || t.ReductionStatus == ReductionStatusEnum.Reducing);
            }
        }

        /// <summary>
        /// Changes the status of ReductionTask records in the database to Canceled
        /// </summary>
        /// <param name="TasksToCancel"></param>
        /// <returns></returns>
        private async Task<bool> CancelReductionTasks(IEnumerable<Guid> TaskIdsToCancel, string userMessage)
        {
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                try
                {
                    foreach (Guid id in TaskIdsToCancel)
                    {
                        ContentReductionTask taskToCancel = await Db.ContentReductionTask
                                                                    .Include(t => t.SelectionGroup)
                                                                    .SingleOrDefaultAsync(t => t.Id == id);

                        taskToCancel.ReductionStatus = ReductionStatusEnum.Canceled;
                        taskToCancel.OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                        {
                            OutcomeReason = MapDbReductionTaskOutcomeReason.Canceled,
                            ReductionTaskId = taskToCancel.Id,
                            SupportMessage = "Canceled programatically in MapDbPublishRunner",
                            UserMessage = userMessage,
                            SelectionGroupName = taskToCancel.SelectionGroup != null 
                                ? taskToCancel.SelectionGroup.GroupName 
                                : default,
                        };
                        Log.Information($"MapDbPublishRunner.CancelReductionTasks(), canceling reduction task {id} programatically in MapDbPublishRunner");
                    };
                    await Db.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Performs all actions required only in the presence of a master content file in the publication request
        /// Note that a publication request can be made without a master content file to update associated files.
        /// </summary>
        /// <param name="masterContentFile"></param>
        private async Task QueueReductionActivityAsync(ContentRelatedFile masterContentFile)
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                // if there are no selection groups for this content, create a master group
                if (!await Db.SelectionGroup.AnyAsync(sg => sg.RootContentItemId == JobDetail.Request.RootContentId))
                {
                    SelectionGroup NewMasterSelectionGroup = new SelectionGroup
                    {
                        RootContentItemId = JobDetail.Request.RootContentId,
                        GroupName = "Master Content Access",
                        IsMaster = true,
                        Id = Guid.NewGuid(),
                        TypeSpecificDetail = JsonConvert.SerializeObject(new object()),
                    };
                    Db.SelectionGroup.Add(NewMasterSelectionGroup);
                    await Db.SaveChangesAsync();
                }
            }

            if (JobDetail.Request.DoesReduce)
            {
                switch (JobDetail.Request.ContentType)
                {
                    case ContentTypeEnum.Qlikview:
                        {
                            string QvSourceDocumentsPath = Configuration.ApplicationConfiguration.GetSection("Storage")["QvSourceDocumentsPath"];

                            // Create a single master hierarchy extraction
                            ContentReductionTask MasterHierarchyTask = new ContentReductionTask
                            {
                                Id = Guid.NewGuid(),  // In normal operation db could generate a value; this is done for unit tests
                                ApplicationUserId = JobDetail.Request.ApplicationUserId,
                                ContentPublicationRequestId = JobDetail.JobId,
                                CreateDateTimeUtc = JobDetail.Request.CreateDateTimeUtc,
                                MasterFilePath = masterContentFile.FullPath,
                                MasterContentChecksum = masterContentFile.Checksum,
                                SelectionGroupId = null,
                                ReductionStatus = ReductionStatusEnum.Queued,
                                TaskAction = TaskActionEnum.HierarchyOnly,
                            };

                            // Queue hierarchy extraction task and wait for completion
                            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                            {
                                Db.ContentReductionTask.Add(MasterHierarchyTask);
                                await Db.SaveChangesAsync();

                                // Wait for hierarchy extraction task to finish
                                while (new ReductionStatusEnum[] { ReductionStatusEnum.Queued, ReductionStatusEnum.Reducing }.Contains(MasterHierarchyTask.ReductionStatus))
                                {
                                    Thread.Sleep(2000);

                                    var EntryInfo = Db.Entry(MasterHierarchyTask);
                                    if (EntryInfo != null) // needed for unit tests, this is not mocked
                                    {
                                        Db.Entry(MasterHierarchyTask).State = EntityState.Detached;
                                    }
                                    MasterHierarchyTask = await Db.ContentReductionTask.FindAsync(MasterHierarchyTask.Id);
                                }

                                // Hierarchy task is no longer waiting to finish processing
                                if (MasterHierarchyTask.ReductionStatus != ReductionStatusEnum.Reduced
                                 || MasterHierarchyTask.MasterContentHierarchyObj == null)
                                {
                                    return;
                                }
                            }

                            var extractedFieldNames = new HashSet<string>(MasterHierarchyTask.MasterContentHierarchyObj.Fields.Select(f => f.FieldName));
                            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                            {
                                var liveHierarchyFieldNames = new HashSet<string>(await Db.HierarchyField.Where(f => f.RootContentItemId == JobDetail.Request.RootContentId).Select(f => f.FieldName).ToListAsync());
                                if (liveHierarchyFieldNames.Any() && !extractedFieldNames.SetEquals(liveHierarchyFieldNames))
                                {
                                    throw new ApplicationException($"New master hierarchy field names ({string.Join(",", extractedFieldNames)}) do not match the live hierarchy field names ({string.Join(",", liveHierarchyFieldNames)}) for this content item");
                                }
                            }

                            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                            {
                                foreach (SelectionGroup SelGrp in await Db.SelectionGroup
                                                                          .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                                          .ToListAsync())
                                {
                                    ContentReductionTask NewTask = new ContentReductionTask
                                    {
                                        Id = Guid.NewGuid(),  // In normal operation db could generate a value; this is done for unit tests
                                        ApplicationUserId = JobDetail.Request.ApplicationUserId,
                                        ContentPublicationRequestId = JobDetail.JobId,
                                        CreateDateTimeUtc = JobDetail.Request.CreateDateTimeUtc,
                                        MasterFilePath = masterContentFile.FullPath,
                                        MasterContentChecksum = masterContentFile.Checksum,
                                        SelectionGroupId = SelGrp.Id,
                                        MasterContentHierarchyObj = MasterHierarchyTask.MasterContentHierarchyObj,
                                    };

                                    if (SelGrp.IsMaster)
                                    {
                                        NewTask.TaskAction = TaskActionEnum.HierarchyOnly;
                                        NewTask.ReductionStatus = ReductionStatusEnum.Reduced;
                                        NewTask.ProcessingStartDateTimeUtc = DateTime.UtcNow;
                                        NewTask.OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                                        {
                                            ProcessingStartedUtc = DateTime.UtcNow,
                                            ReductionTaskId = NewTask.Id,
                                            SelectionGroupName = SelGrp.GroupName,
                                            OutcomeReason = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned,
                                            UserMessage = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned.GetDisplayDescriptionString(),
                                        };
                                    }
                                    else
                                    {
                                        NewTask.TaskAction = TaskActionEnum.ReductionOnly;
                                        NewTask.SelectionCriteriaObj = await ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroupAsync(Db, SelGrp.Id);
                                        if (NewTask.SelectionCriteriaObj.Fields.Any(f => f.Values.Any(v => v.SelectionStatus)))
                                        {
                                            NewTask.ReductionStatus = ReductionStatusEnum.Queued;
                                        }
                                        else
                                        {
                                            // There are no values selected
                                            NewTask.ReductionStatus = ReductionStatusEnum.Warning;
                                            NewTask.OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                                            {
                                                ProcessingStartedUtc = DateTime.UtcNow,
                                                ReductionTaskId = NewTask.Id,
                                                SelectionGroupName = SelGrp.GroupName,
                                                OutcomeReason = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                                                UserMessage = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues.GetDisplayDescriptionString(),
                                            };
                                        }
                                    }

                                    Db.ContentReductionTask.Add(NewTask);
                                    await Db.SaveChangesAsync();
                                }
                            }
                        }
                        break;

                    case ContentTypeEnum.PowerBi:
                        Log.Information($"Reducing Power BI document");
                        // TODO maybe nothing is needed here
                        break;
                }
            }
            else
            {
                // nothing to queue, but do not resolve publication request status here
            }
        }

        private async Task GeneratePbiRlsReductionTaskRecords()
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                // if there are no selection groups for this content, create a master group
                if (!await Db.SelectionGroup.AnyAsync(sg => sg.RootContentItemId == JobDetail.Request.RootContentId))
                {
                    SelectionGroup NewMasterSelectionGroup = new SelectionGroup
                    {
                        RootContentItemId = JobDetail.Request.RootContentId,
                        GroupName = "Master Content Access",
                        IsMaster = true,
                        Id = Guid.NewGuid(),
                        TypeSpecificDetail = JsonConvert.SerializeObject(new PowerBiSelectionGroupProperties()),
                    };
                    Db.SelectionGroup.Add(NewMasterSelectionGroup);
                    await Db.SaveChangesAsync();
                }
            }

            using (ApplicationDbContext db = new ApplicationDbContext(ContextOptions))
            {
                List<SelectionGroup> selectionGroups = await db.SelectionGroup
                                                               .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                               .ToListAsync();

                foreach (SelectionGroup group in selectionGroups)
                {
                    DateTime now = DateTime.UtcNow;

                    var newTaskRecord = new ContentReductionTask
                    {
                        Id = Guid.NewGuid(),
                        SelectionGroupId = group.Id,
                        ApplicationUserId = JobDetail.Request.ApplicationUserId,
                        ContentPublicationRequestId = JobDetail.JobId,
                        CreateDateTimeUtc = now,
                        ProcessingStartDateTimeUtc = now,
                        TaskAction = TaskActionEnum.Unspecified,
                        ResultFilePath = null,
                        MasterFilePath = string.Empty,
                    };

                    var outcomeMetadata = new ReductionTaskOutcomeMetadata
                    {
                        ProcessingStartedUtc = now,
                        ReductionTaskId = newTaskRecord.Id,
                        SelectionGroupName = group.GroupName,
                    };

                    try
                    {
                        List<HierarchyFieldValue> existingSelectedValues = db.HierarchyFieldValue
                                                                             .Where(v => group.SelectedHierarchyFieldValueList != null && group.SelectedHierarchyFieldValueList.Contains(v.Id))
                                                                             .ToList();

                        ContentReductionHierarchy<ReductionFieldValue> newContentHierarchy = new ContentReductionHierarchy<ReductionFieldValue>
                        {
                            RootContentItemId = JobDetail.Request.RootContentId,
                        };

                        newContentHierarchy.Fields.Add(new ReductionField<ReductionFieldValue>
                        {
                            Id = Guid.Empty,
                            FieldName = "Roles",
                            DisplayName = "Roles",
                            StructureType = FieldStructureType.Flat,
                            Values = ((PowerBiPublicationProperties)JobDetail.Request.TypeSpecificDetail).RoleList
                                .Select(r => new ReductionFieldValue { Value = r })
                                .ToList(),
                        });

                        newTaskRecord.MasterContentHierarchyObj = newContentHierarchy;
                        newTaskRecord.ReducedContentHierarchyObj = group.IsMaster
                            ? null
                            : newContentHierarchy;
                        if (group.IsMaster)
                        {
                            newTaskRecord.SelectionCriteriaObj = null;

                            newTaskRecord.ReductionStatus = ReductionStatusEnum.Reduced;
                            newTaskRecord.ReductionStatusMessage = "";
                            outcomeMetadata.OutcomeReason = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned;
                            outcomeMetadata.UserMessage = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned.GetDisplayDescriptionString();
                        }
                        else
                        {
                            HierarchyField existingHierarchyField = await db.HierarchyField
                                                                            .Include(f => f.HierarchyFieldValues)
                                                                            .SingleOrDefaultAsync(f => f.FieldName == "Roles" &&
                                                                                                  f.RootContentItemId == JobDetail.Request.RootContentId);

                            // If there is no stored hierarchy the reduction gets warning status
                            if (existingHierarchyField is null)
                            {
                                newTaskRecord.ReductionStatus = ReductionStatusEnum.Warning;
                                newTaskRecord.ReductionStatusMessage = "There is currently no active role list for this content item";
                                outcomeMetadata.OutcomeReason = MapDbReductionTaskOutcomeReason.SelectionForInvalidFieldName;
                                outcomeMetadata.UserMessage = "There is currently no active role list for this content item";
                                outcomeMetadata.SupportMessage = "There is currently no active (live) role list for this content item";
                            }
                            else if (!existingSelectedValues.Select(v => v.Value).Any(r => ((PowerBiPublicationProperties)JobDetail.Request.TypeSpecificDetail).RoleList.Contains(r)))
                            {
                                // There are no values selected
                                newTaskRecord.ReductionStatus = ReductionStatusEnum.Warning;
                                newTaskRecord.ReductionStatusMessage = MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent.GetDisplayDescriptionString();
                                outcomeMetadata.OutcomeReason = MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent;
                                outcomeMetadata.UserMessage = MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent.GetDisplayDescriptionString();
                                outcomeMetadata.SupportMessage = "No roles of the new publication are selected for this selection group";
                                newTaskRecord.SelectionCriteriaObj = new ContentReductionHierarchy<ReductionFieldValueSelection>
                                {
                                    RootContentItemId = JobDetail.Request.RootContentId,
                                    Fields = new List<ReductionField<ReductionFieldValueSelection>> { new ReductionField<ReductionFieldValueSelection>
                                        // always one field for Power BI
                                        {
                                            Id = existingHierarchyField.Id,
                                            FieldName = existingHierarchyField.FieldName,
                                            DisplayName = existingHierarchyField.FieldDisplayName,
                                            StructureType = existingHierarchyField.StructureType,
                                            ValueDelimiter = existingHierarchyField.FieldDelimiter,
                                        }},
                                };
                            }
                            else
                            {
                                var newSelectionList = new ContentReductionHierarchy<ReductionFieldValueSelection>
                                {
                                    RootContentItemId = JobDetail.Request.RootContentId,
                                    Fields = new List<ReductionField<ReductionFieldValueSelection>> { new ReductionField<ReductionFieldValueSelection>
                                    // always one field for Power BI
                                    {
                                        Id = existingHierarchyField.Id,
                                        FieldName = existingHierarchyField.FieldName,
                                        DisplayName = existingHierarchyField.FieldDisplayName,
                                        StructureType = existingHierarchyField.StructureType,
                                        ValueDelimiter = existingHierarchyField.FieldDelimiter,
                                    }},
                                };

                                foreach (string roleName in ((PowerBiPublicationProperties)JobDetail.Request.TypeSpecificDetail).RoleList)
                                {
                                    newSelectionList.Fields[0].Values.Add(new ReductionFieldValueSelection
                                    {
                                        Id = existingHierarchyField.HierarchyFieldValues.SingleOrDefault(v => v.Value == roleName)?.Id ?? Guid.Empty,
                                        Value = roleName,
                                        SelectionStatus = existingSelectedValues.Select(v => v.Value).Contains(roleName),
                                    });
                                }

                                newTaskRecord.SelectionCriteriaObj = newSelectionList;

                                newTaskRecord.ReductionStatus = ReductionStatusEnum.Reduced;
                                newTaskRecord.ReductionStatusMessage = "";

                                outcomeMetadata.OutcomeReason = MapDbReductionTaskOutcomeReason.Success;
                                outcomeMetadata.UserMessage = newTaskRecord.ReductionStatusMessage;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        newTaskRecord.ReductionStatus = ReductionStatusEnum.Error;
                        newTaskRecord.ReductionStatusMessage = $"Error while storing reduced hierarchy information for Power BI content";

                        outcomeMetadata.OutcomeReason = MapDbReductionTaskOutcomeReason.UnspecifiedError;
                        outcomeMetadata.UserMessage = newTaskRecord.ReductionStatusMessage;
                        outcomeMetadata.SupportMessage = GlobalFunctions.LoggableExceptionString(ex, $"Failed to populate ContentReductionTask record for Power BI role assignment processing, selection group {group.Id} ({group.GroupName})", IncludeStackTrace: true);

                        Log.Error(ex, $"Failed to populate ContentReductionTask record for Power BI role assignment processing, selection group {group.Id} ({group.GroupName})");
                    }

                    newTaskRecord.OutcomeMetadataObj = outcomeMetadata;

                    db.ContentReductionTask.Add(newTaskRecord);
                } // foreach group

                await db.SaveChangesAsync();
            }

        }
    }
}
