/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
using System.Threading.Tasks;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using ContentPublishingLib.JobRunners;
using TestResourcesLib;
using Newtonsoft.Json;

namespace ContentPublishingLib.JobMonitors
{
    public class MapDbPublishJobMonitor : JobMonitorBase
    {
        internal class PublishJobTrackingItem
        {
            internal Task<PublishJobDetail> task;
            internal CancellationTokenSource tokenSource;
            internal Guid requestId;
        }

        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private List<PublishJobTrackingItem> ActivePublicationRunnerItems = new List<PublishJobTrackingItem>();

        private TimeSpan TaskAgeBeforeExecution
        {
            get
            {
                int TaskAgeSec;
                try
                {
                    TaskAgeSec = int.Parse(Configuration.ApplicationConfiguration["TaskAgeBeforeExecutionSeconds"]);
                }
                catch
                {
                    TaskAgeSec = 30;
                }
                return TimeSpan.FromSeconds(TaskAgeSec);
            }
        }

        private int MaxParallelRequests
        {
            get
            {
                int MaxTasks;
                try
                {
                    MaxTasks = int.Parse(Configuration.ApplicationConfiguration["MaxParallelTasks"]);
                }
                catch
                {
                    MaxTasks = 1;
                }
                return MaxTasks;
            }
        }

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
                _ConnectionString = value;
            }
            private get
            {
                return _ConnectionString;
            }
        }
        private string _ConnectionString;

        /// <summary>
        /// Starts the worker thread of this object
        /// </summary>
        /// <param name="Token">Allows the worker to react to task cancellation by the caller</param>
        /// <returns></returns>
        public override Task Start(CancellationToken Token)
        {
            if (ContextOptions == null && MockContext == null)
            {
                throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
            }

            return Task.Run(() => JobMonitorThreadMain(Token), Token);
        }

        public override void JobMonitorThreadMain(CancellationToken Token)
        {
            MethodBase Method = MethodBase.GetCurrentMethod();
            while (!Token.IsCancellationRequested)
            {
                // Remove completed tasks from the RunningTasks collection. 
                // .ToList() is needed because the body changes the original List. 
                foreach (PublishJobTrackingItem CompletedPublishRunnerItem in ActivePublicationRunnerItems.Where(t => t.task.IsCompleted).ToList())
                {
                    UpdateRequest(CompletedPublishRunnerItem.task.Result);
                    ActivePublicationRunnerItems.Remove(CompletedPublishRunnerItem);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (ActivePublicationRunnerItems.Count < MaxParallelRequests)
                {
                    List<ContentPublicationRequest> Responses = GetReadyRequests(MaxParallelRequests - ActivePublicationRunnerItems.Count);

                    foreach (ContentPublicationRequest DbRequest in Responses)
                    {
                        Task<PublishJobDetail> NewTask = null;
                        CancellationTokenSource cancelSource = new CancellationTokenSource();

                        // Do I need a switch on ContentType?  (example in MapDbReductionJobMonitor)
                        MapDbPublishRunner Runner;
                        using (ApplicationDbContext Db = MockContext != null
                                                         ? MockContext.Object
                                                         : new ApplicationDbContext(ContextOptions))
                        {
                            Runner = new MapDbPublishRunner
                            {
                                JobDetail = PublishJobDetail.New(DbRequest, Db),
                            };
                        }
                        if (MockContext != null)
                        {
                            Runner.MockContext = MockContext;
                            Runner.SetTestAuditLogger(MockAuditLogger.New().Object);
                        }
                        else
                        {
                            Runner.ConnectionString = ConnectionString;
                        }

                        NewTask = Task.Run(() => Runner.Execute(cancelSource.Token), cancelSource.Token);

                        if (NewTask != null)
                        {
                            ActivePublicationRunnerItems.Add(new PublishJobTrackingItem { requestId = DbRequest.Id, task = NewTask, tokenSource = cancelSource });
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            GlobalFunctions.TraceWriteLine($"{Method.ReflectedType.Name}.{Method.Name} stopping {ActivePublicationRunnerItems.Count} active JobRunners, waiting up to {StopWaitTimeSeconds}");

            if (ActivePublicationRunnerItems.Count != 0)
            {
                ActivePublicationRunnerItems.ForEach(t => t.tokenSource.Cancel());

                DateTime WaitStart = DateTime.Now;
                while (DateTime.Now - WaitStart < StopWaitTimeSeconds)
                {
                    GlobalFunctions.TraceWriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} waiting for {ActivePublicationRunnerItems.Count} running tasks to complete");

                    int CompletedTaskIndex = Task.WaitAny(ActivePublicationRunnerItems.Select(t => t.task).ToArray(), new TimeSpan(StopWaitTimeSeconds.Ticks / 100));
                    if (CompletedTaskIndex > -1)
                    {
                        ActivePublicationRunnerItems.RemoveAt(CompletedTaskIndex);
                    }

                    if (ActivePublicationRunnerItems.Count == 0)
                    {
                        GlobalFunctions.TraceWriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} all publication runners terminated successfully");
                        break;
                    }
                }

                foreach (var Item in ActivePublicationRunnerItems)
                {
                    GlobalFunctions.TraceWriteLine($"{Method.ReflectedType.Name}.{Method.Name} after timer expired, task {Item.requestId.ToString()} not completed");
                }
            }

            Token.ThrowIfCancellationRequested();
            GlobalFunctions.TraceWriteLine($"{Method.ReflectedType.Name}.{Method.Name} returning");
        }

        /// <summary>
        /// Query the database for tasks to be run
        /// </summary>
        /// <param name="ReturnNoMoreThan">The maximum number of records to return</param>
        /// <returns></returns>
        internal List<ContentPublicationRequest> GetReadyRequests(int ReturnNoMoreThan)
        {
            if (ReturnNoMoreThan < 1)
            {
                return new List<ContentPublicationRequest>();
            }

            using (ApplicationDbContext Db = MockContext != null
                                             ? MockContext.Object
                                             : new ApplicationDbContext(ContextOptions))
            using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
            {
                try
                {
                    List<ContentPublicationRequest> TopItems = Db.ContentPublicationRequest.Where(r => DateTime.UtcNow - r.CreateDateTimeUtc > TaskAgeBeforeExecution)
                                                                                 .Where(r => r.RequestStatus == PublicationStatus.Queued)
                                                                                 .Include(r => r.RootContentItem)
                                                                                 .OrderBy(r => r.CreateDateTimeUtc)
                                                                                 .Take(ReturnNoMoreThan)
                                                                                 .ToList();
                    if (TopItems.Count > 0)
                    {
                        TopItems.ForEach(r => r.RequestStatus = PublicationStatus.Processing);
                        Db.ContentPublicationRequest.UpdateRange(TopItems);
                        Db.SaveChanges();
                        Transaction.Commit();
                    }
                    return TopItems;
                }
                catch (Exception e)
                {
                    GlobalFunctions.TraceWriteLine(GlobalFunctions.LoggableExceptionString(e, $"Failed to query MAP database for available tasks:"));
                    return new List<ContentPublicationRequest>();
                }
            }
        }

        /// <summary>
        /// Updates the MAP database ContentPublicationRequest record with the outcome of <...>PublishRunner processing.
        /// </summary>
        /// <param name="Result">Contains the field values to be saved. All field values will be saved.</param>
        /// <returns></returns>
        private bool UpdateRequest(PublishJobDetail JobDetail)
        {
            if (JobDetail == null || JobDetail.Result == null || JobDetail.JobId == Guid.Empty)
            {
                MethodBase Method = MethodBase.GetCurrentMethod();
                string Msg = $"{Method.ReflectedType.Name}.{Method.Name} unusable argument";
                GlobalFunctions.TraceWriteLine(Msg);
                return false;
            }

            try
            {
                // Use a transaction so that there is no concurrency issue after we get the current db record
                using (ApplicationDbContext Db = MockContext != null
                                                 ? MockContext.Object
                                                 : new ApplicationDbContext(ContextOptions))
                using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
                {
                    ContentPublicationRequest DbRequest = Db.ContentPublicationRequest.Find(JobDetail.JobId);

                    if (DbRequest == null)
                    {
                        return false;
                    }

                    // Canceled here implies that the application does not want the update
                    if (DbRequest.RequestStatus == PublicationStatus.Canceled)
                    {
                        List<ContentReductionTask> RelatedTasks = Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == DbRequest.Id).ToList();
                        RelatedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);
                        Db.ContentReductionTask.UpdateRange(RelatedTasks);
                        Db.SaveChanges();
                        Transaction.Commit();
                        return true;
                    }

                    switch (JobDetail.Status)
                    {
                        case PublishJobDetail.JobStatusEnum.Unspecified:
                            DbRequest.RequestStatus = PublicationStatus.Unknown;
                            break;
                        case PublishJobDetail.JobStatusEnum.Error:
                            DbRequest.RequestStatus = PublicationStatus.Error;
                            break;
                        case PublishJobDetail.JobStatusEnum.Success:
                            DbRequest.RequestStatus = PublicationStatus.Processed;
                            DbRequest.ReductionRelatedFilesObj = new List<ReductionRelatedFiles>
                            {
                                new ReductionRelatedFiles{ MasterContentFile = JobDetail.Request.MasterContentFile, ReducedContentFileList = JobDetail.Result.ResultingRelatedFiles }
                            };
                            break;
                        case PublishJobDetail.JobStatusEnum.Canceled:
                            DbRequest.RequestStatus = PublicationStatus.Canceled;
                            break;
                        default:
                            GlobalFunctions.TraceWriteLine("Unsupported job result status in MapDbPublishJobMonitor.UpdateTask().");
                            return false;
                    }

                    DbRequest.StatusMessage = JobDetail.Result.StatusMessage;

                    Db.ContentPublicationRequest.Update(DbRequest);
                    Db.SaveChanges();
                    Transaction.Commit();
                }

                return true;
            }
            catch (Exception e)
            {
                GlobalFunctions.TraceWriteLine(GlobalFunctions.LoggableExceptionString(e, "Failed to update ContentPublishRequest in database:", true, true));
                return false;
            }
        }
    }
}
