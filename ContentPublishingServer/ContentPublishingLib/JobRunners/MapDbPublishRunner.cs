/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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
                ConnectionString = Configuration.GetConnectionString(value);
            }
        }

        internal TimeSpan TimeLimit { get; set; } = new TimeSpan(6, 0, 0);  // TODO Get the timeout from configuration)

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
            MethodBase Method = MethodBase.GetCurrentMethod();
            DateTime WaitEndUtc = DateTime.UtcNow + TimeLimit;

            _CancellationToken = cancellationToken;
            // AuditLog would not be null during a test run where it may be initialized earlier
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            try
            {
                if (JobDetail.Request.MasterContentFile != null)
                {
                    QueueReductionActivity(JobDetail.Request.MasterContentFile);
                }

                // Wait for any/all related reduction tasks to complete
                for (int PendingTaskCount = await CountPendingReductionTasks(); PendingTaskCount > 0; PendingTaskCount = await CountPendingReductionTasks())
                {
                    if (DateTime.UtcNow > WaitEndUtc)
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

                // Check the actual status of reduction tasks to assign publication status
                if (AllRelatedReductionTasks.All(t => t.ReductionStatus == ReductionStatusEnum.Reduced))
                {
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;

                    #region Log audit event
                    var DetailObj = new
                    {
                        PublicationRequestId = JobDetail.JobId,
                        JobDetail.Request.DoesReduce,
                        RequestingUser = JobDetail.Request.ApplicationUserId,
                        ReductionTasks = AllRelatedReductionTasks.Select(t => t.Id.ToString("D")).ToArray(),
                    };
                    AuditLog.Log(AuditEventType.PublicationRequestProcessingSuccess.ToEvent(DetailObj));
                    #endregion

                }
                else
                {
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                }
            }
            catch (OperationCanceledException e)
            {
                string msg = GlobalFunctions.LoggableExceptionString(e);
                GlobalFunctions.TraceWriteLine($"{Method.ReflectedType.Name}.{Method.Name} {msg}");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;
                JobDetail.Result.StatusMessage = msg;
            }
            catch (Exception e)
            {
                string msg = GlobalFunctions.LoggableExceptionString(e);
                GlobalFunctions.TraceWriteLine($"{Method.ReflectedType.Name}.{Method.Name} {msg}");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                JobDetail.Result.StatusMessage = msg;
            }

            return JobDetail;
        }

        private async Task<int> CountPendingReductionTasks()
        {
            using (ApplicationDbContext Db = GetDbContext())
            {
                List<ContentReductionTask> AllRelatedReductionTasks = await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToListAsync();

                if (_CancellationToken.IsCancellationRequested)
                {
                    List<ContentReductionTask> QueuedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Queued).ToList();
                    await CancelReductionTasks(QueuedTasks);
                    JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;
                    _CancellationToken.ThrowIfCancellationRequested();
                }

                List<ContentReductionTask> FailedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Error).ToList(); 
                if (FailedTasks.Any())
                {
                    // Cancel any task still queued
                    List<ContentReductionTask> QueuedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Queued).ToList();
                    await CancelReductionTasks(QueuedTasks);

                    string Msg = $"Publication request terminating due to error in related reduction task(s):{Environment.NewLine}  {string.Join("  " + Environment.NewLine, FailedTasks.Select(t => t.Id.ToString() + " : " + t.ReductionStatusMessage))}";
                    GlobalFunctions.TraceWriteLine(Msg);

                    throw new ApplicationException(Msg);
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
                    TasksToCancel.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);
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
        /// Note that a publication request can be made without a mast content file to update associated files.
        /// </summary>
        /// <param name="contentRelatedFile"></param>
        private void QueueReductionActivity(ContentRelatedFile contentRelatedFile)
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = GetDbContext())
            {
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
                        NewMasterSelectionGroup.Id = Db.SelectionGroup.Max(sg => sg.Id) + 1;
                    }
                    Db.SelectionGroup.Add(NewMasterSelectionGroup);
                    Db.SaveChanges();
                }
            }

            if (JobDetail.Request.DoesReduce)
            {
                string QvSourceDocumentsPath = Configuration.ApplicationConfiguration.GetSection("Storage")["QvSourceDocumentsPath"];

                using (ApplicationDbContext Db = GetDbContext())
                {
                    bool MasterHierarchyHasBeenRequested = false;

                    foreach (SelectionGroup SelGrp in Db.SelectionGroup
                                                        .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                        .ToList())
                    {
                        if (SelGrp.IsMaster && MasterHierarchyHasBeenRequested)
                        {
                            // Only handle IsMaster SelectionGroups once
                            continue;
                        }

                        ContentReductionTask NewTask = new ContentReductionTask
                        {
                            Id = Guid.NewGuid(),  // In normal operation db could generate a value; this is done for unit tests
                            ApplicationUserId = JobDetail.Request.ApplicationUserId,
                            ContentPublicationRequestId = JobDetail.JobId,
                            CreateDateTimeUtc = DateTime.UtcNow,  // TODO later: Figure out how to avoid delay in starting the reduction task. 
                            MasterFilePath = contentRelatedFile.FullPath,
                            MasterContentChecksum = contentRelatedFile.Checksum,
                            ReductionStatus = ReductionStatusEnum.Queued,
                            SelectionGroupId = SelGrp.Id,
                        };

                        if (SelGrp.IsMaster)
                        {
                            NewTask.TaskAction = TaskActionEnum.HierarchyOnly;
                            MasterHierarchyHasBeenRequested = true;
                        }
                        else
                        {
                            NewTask.SelectionCriteriaObj = ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(Db, SelGrp.Id);
                            NewTask.TaskAction = TaskActionEnum.HierarchyAndReduction;
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
