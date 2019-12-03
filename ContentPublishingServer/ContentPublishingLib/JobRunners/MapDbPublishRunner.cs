/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using AuditLogLib;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Moq;
using AuditLogLib.Event;

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
        internal string ConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                ContextOptions = ContextBuilder.Options;
            }
        }

        private Mock<ApplicationDbContext> _MockContext = null;
        public Mock<ApplicationDbContext> MockContext
        {
            protected get
            {
                return _MockContext;
            }
            set
            {
                AssertTesting();
                _MockContext = value;
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
        /// Gets an appropriate ApplicationDbContext object, depending on whether a Mocked context is assigned (test run)
        /// </summary>
        /// <returns></returns>
        protected ApplicationDbContext GetDbContext()
        {
            return MockContext != null
                 ? MockContext.Object
                 : new ApplicationDbContext(ContextOptions);
        }

        /// <summary>
        /// Main functional entry point for the runner, 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PublishJobDetail> Execute(CancellationToken cancellationToken)
        {
            long limitTicksPerReductionTask = new TimeSpan(0, 10, 0).Ticks;
            MethodBase Method = MethodBase.GetCurrentMethod();

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
                if (JobDetail.Request.MasterContentFile != null && !JobDetail.Request.SkipReductionTaskQueueing)
                {
                    QueueReductionActivity(JobDetail.Request.MasterContentFile);
                }

                int PendingTaskCount = await CountPendingReductionTasks();
                TimeSpan timeLimit = TimeSpan.FromTicks(limitTicksPerReductionTask * PendingTaskCount);

                // Wait for any/all related reduction tasks to complete
                for ( ; PendingTaskCount > 0; PendingTaskCount = await CountPendingReductionTasks())
                {
                    if (DateTime.UtcNow > StartUtc + timeLimit)
                    {
                        using (ApplicationDbContext Db = GetDbContext())
                        {
                            var QueuedTasks = Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId)
                                                                     .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued)
                                                                     .ToList();
                            await CancelReductionTasks(QueuedTasks);
                        }

                        // throw so that the exception message gets recorded in the ContentPublicationRequest.StatusMessage field
                        throw new ApplicationException($"{Method.DeclaringType.Name}.{Method.Name} timed out waiting for {PendingTaskCount} pending reduction tasks");
                    }

                    Thread.Sleep(1000);
                }

                List<ContentReductionTask> AllRelatedReductionTasks = null;
                using (ApplicationDbContext Db = GetDbContext())
                {
                    AllRelatedReductionTasks = Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToList();
                }

                var unhandleableErrors = AllRelatedReductionTasks
                    .Where(t => t.ReductionStatus == ReductionStatusEnum.Error)
                    .Where(t => t.OutcomeMetadataObj.OutcomeReason.PreventsPublication());
                if (unhandleableErrors.Any())
                {
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                }
                else if (AllRelatedReductionTasks.Any()
                    && AllRelatedReductionTasks.All(t => t.ReductionStatus == ReductionStatusEnum.Canceled))
                {
                    // If a publication timeout happens queued tasks are canceled but we won't get here because that also throws ApplicationException
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;

                    #region Log audit event
                    var DetailObj = new
                    {
                        PublicationRequestId = JobDetail.JobId,
                        JobDetail.Request.DoesReduce,
                    };
                    AuditLog.Log(AuditEventType.ContentPublicationRequestCanceled.ToEvent(DetailObj));
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
                    AuditLog.Log(AuditEventType.PublicationRequestProcessingSuccess.ToEvent(DetailObj));
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
                string msg = GlobalFunctions.LoggableExceptionString(e);
                Log.Error(e, $"Exception from MapDbPublishRunner");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                JobDetail.Result.StatusMessage = msg;
            }
            finally
            {
                using (ApplicationDbContext Db = GetDbContext())
                {
                    foreach (ContentReductionTask RelatedTask in Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToList())
                    {
                        ReductionTaskOutcomeMetadata TaskOutcome = RelatedTask.OutcomeMetadataObj;
                        if (TaskOutcome.ReductionTaskId == Guid.Empty)
                        {
                            TaskOutcome.ReductionTaskId = RelatedTask.Id;
                        }
                        if (TaskOutcome.OutcomeReason == MapDbReductionTaskOutcomeReason.Success ||
                            // TODO Add new Warning status here
                            TaskOutcome.OutcomeReason == MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned)
                        {
                            JobDetail.Result.ReductionTaskSuccessList.Add(TaskOutcome);
                        }
                        else
                        {
                            JobDetail.Result.ReductionTaskFailList.Add(TaskOutcome);
                        }
                        Log.Debug($"From MapDbPublishRunner, recording OutcomeMetadata of related reduction task {RelatedTask.Id}");
                    }
                }

                JobDetail.Result.ElapsedTime = DateTime.UtcNow - JobDetail.Result.StartDateTime;
            }

            return JobDetail;
        }

        private async Task<int> CountPendingReductionTasks()
        {
            using (ApplicationDbContext Db = GetDbContext())
            {
                PublicationStatus RequestStatus = Db.ContentPublicationRequest.Where(r => r.Id == JobDetail.JobId)
                                                                              .Select(r => r.RequestStatus)
                                                                              .FirstOrDefault();  // default is PublicationStatus.Unknown
                List<ContentReductionTask> AllRelatedReductionTasks = await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToListAsync();

                if (_CancellationToken.IsCancellationRequested || RequestStatus == PublicationStatus.Canceled)
                {
                    await CancelReductionTasks(AllRelatedReductionTasks);
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
        private async Task<bool> CancelReductionTasks(List<ContentReductionTask> TasksToCancel)
        {
            using (ApplicationDbContext Db = GetDbContext())
            {
                try
                {
                    TasksToCancel.ForEach(t =>
                    {
                        t.ReductionStatus = ReductionStatusEnum.Canceled;
                        t.OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                        {
                            OutcomeReason = MapDbReductionTaskOutcomeReason.Canceled,
                            ReductionTaskId = t.Id,
                        };
                    });
                    Db.ContentReductionTask.UpdateRange(TasksToCancel);
                    await Db.SaveChangesAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Performs all actions required only in the presence of a master content file in the publication request
        /// Note that a publication request can be made without a master content file to update associated files.
        /// </summary>
        /// <param name="contentRelatedFile"></param>
        private void QueueReductionActivity(ContentRelatedFile contentRelatedFile)
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = GetDbContext())
            {
                // if there are no selection groups for this content, create a master group
                if (!Db.SelectionGroup.Any(sg => sg.RootContentItemId == JobDetail.Request.RootContentId))
                {
                    SelectionGroup NewMasterSelectionGroup = new SelectionGroup
                    {
                        RootContentItemId = JobDetail.Request.RootContentId,
                        GroupName = "Master Content Access",
                        IsMaster = true,
                    };
                    // for Mocked DbSet (unit testing) there is no default expression for Id 
                    if (MockContext != null)  
                    {
                        NewMasterSelectionGroup.Id = Guid.NewGuid();
                    }
                    Db.SelectionGroup.Add(NewMasterSelectionGroup);
                    Db.SaveChanges();
                }
            }

            if (JobDetail.Request.DoesReduce)
            {
                string QvSourceDocumentsPath = Configuration.ApplicationConfiguration.GetSection("Storage")["QvSourceDocumentsPath"];

                // Create a single master hierarchy extraction
                ContentReductionTask MasterHierarchyTask = new ContentReductionTask
                {
                    Id = Guid.NewGuid(),  // In normal operation db could generate a value; this is done for unit tests
                    ApplicationUserId = JobDetail.Request.ApplicationUserId,
                    ContentPublicationRequestId = JobDetail.JobId,
                    CreateDateTimeUtc = JobDetail.Request.CreateDateTimeUtc,
                    MasterFilePath = contentRelatedFile.FullPath,
                    MasterContentChecksum = contentRelatedFile.Checksum,
                    SelectionGroupId = null,
                    ReductionStatus = ReductionStatusEnum.Queued,
                    TaskAction = TaskActionEnum.HierarchyOnly,
                };

                // Queue hierarchy extraction task and wait for completion
                using (ApplicationDbContext Db = GetDbContext())
                {
                    Db.ContentReductionTask.Add(MasterHierarchyTask);
                    Db.SaveChanges();

                    // Wait for hierarchy extraction task to finish
                    while (new ReductionStatusEnum[] { ReductionStatusEnum.Queued, ReductionStatusEnum.Reducing }.Contains(MasterHierarchyTask.ReductionStatus))
                    {
                        Thread.Sleep(2000);

                        var EntryInfo = Db.Entry(MasterHierarchyTask);
                        if (EntryInfo != null) // needed for unit tests, this is not mocked
                        {
                            Db.Entry(MasterHierarchyTask).State = EntityState.Detached;
                        }
                        MasterHierarchyTask = Db.ContentReductionTask.Find(MasterHierarchyTask.Id);
                    }

                    // Hierarchy task is no longer waiting to finish processing
                    if (MasterHierarchyTask.ReductionStatus != ReductionStatusEnum.Reduced
                     || MasterHierarchyTask.MasterContentHierarchyObj == null)
                    {
                        return;
                    }
                }

                using (ApplicationDbContext Db = GetDbContext())
                {
                    foreach (SelectionGroup SelGrp in Db.SelectionGroup
                                                        .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                        .ToList())
                    {
                        ContentReductionTask NewTask = new ContentReductionTask
                        {
                            Id = Guid.NewGuid(),  // In normal operation db could generate a value; this is done for unit tests
                            ApplicationUserId = JobDetail.Request.ApplicationUserId,
                            ContentPublicationRequestId = JobDetail.JobId,
                            CreateDateTimeUtc = JobDetail.Request.CreateDateTimeUtc,
                            MasterFilePath = contentRelatedFile.FullPath,
                            MasterContentChecksum = contentRelatedFile.Checksum,
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
                                OutcomeReason = MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned,
                                ReductionTaskId = NewTask.Id,
                                ProcessingStartedUtc = DateTime.UtcNow,
                            };
                        }
                        else
                        {
                            NewTask.TaskAction = TaskActionEnum.ReductionOnly;
                            NewTask.SelectionCriteriaObj = ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(Db, SelGrp.Id);
                            if (NewTask.SelectionCriteriaObj.Fields.Any(f => f.Values.Any(v => v.SelectionStatus)))
                            {
                                NewTask.ReductionStatus = ReductionStatusEnum.Queued;
                            }
                            else
                            {
                                // There are no values selected
                                NewTask.ReductionStatus = ReductionStatusEnum.Error;
                                NewTask.OutcomeMetadataObj = new ReductionTaskOutcomeMetadata
                                {
                                    OutcomeReason = MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                                    ReductionTaskId = NewTask.Id,
                                };
                            }
                        }

                        Db.ContentReductionTask.Add(NewTask);
                        Db.SaveChanges();
                    }
                }
            }
            else
            {
                // nothing to queue, but do not resolve publication request status here
            }
        }
    }
}
