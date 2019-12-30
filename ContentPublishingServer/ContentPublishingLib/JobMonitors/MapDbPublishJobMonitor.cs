/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A queue monitor to initiate processing of appropriate publication requests
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
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
        public enum MapDbPublishJobMonitorType
        {
            ReducingPublications,
            NonReducingPublications,
        }

        /// <summary>
        /// ctor, requires a type specifier to instantiate this class
        /// </summary>
        /// <param name="Type"></param>
        public MapDbPublishJobMonitor(MapDbPublishJobMonitorType Type)
        {
            JobMonitorType = Type;
        }

        internal class PublishJobTrackingItem
        {
            internal Task<PublishJobDetail> task;
            internal CancellationTokenSource tokenSource;
            internal Guid requestId;
        }

        override internal string MaxConcurrentRunnersConfigKey { get; } = "MaxSimultaneousRequests";

        private MapDbPublishJobMonitorType JobMonitorType { get; set; }

        public Mutex QueueMutex { private get; set; }

        private bool IsTestMode { get { return MockContext != null; } }

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
        /// <param name="Token">Allows the worker to react to task cancellation initiated in another thread</param>
        /// <returns>The Task in which this instance's JobMonitorThreadMain method is running</returns>
        public override Task Start(CancellationToken Token)
        {
            if (ContextOptions == null && !IsTestMode)
            {
                throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
            }

            CleanupOnStart();

            return Task.Run(() => JobMonitorThreadMain(Token), Token);
        }

        public override void JobMonitorThreadMain(CancellationToken Token)
        {
            // Main loop
            while (!Token.IsCancellationRequested)
            {
                // Remove completed tasks from the RunningTasks collection. 
                // .ToList() is needed because the body changes the original List. 
                foreach (PublishJobTrackingItem CompletedPublishRunnerItem in ActivePublicationRunnerItems.Where(t => t.task.IsCompleted).ToList())
                {
                    Log.Information($"PublishJobMonitor({JobMonitorType.ToString()}) completed processing for PublicationRequestId {CompletedPublishRunnerItem.requestId.ToString()}");
                    UpdateRequest(CompletedPublishRunnerItem.task.Result);
                    ActivePublicationRunnerItems.Remove(CompletedPublishRunnerItem);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (ActivePublicationRunnerItems.Count < MaxConcurrentRunners)
                {
                    if (QueueMutex.WaitOne(new TimeSpan(0, 0, 20)))
                    {
                        List<ContentPublicationRequest> Responses = GetReadyRequests(MaxConcurrentRunners - ActivePublicationRunnerItems.Count);

                        foreach (ContentPublicationRequest DbRequest in Responses)
                        {
                            LaunchPublishRunnerForRequest(DbRequest);
                        }

                        QueueMutex.ReleaseMutex();
                        Thread.Sleep(500 * (1 + JobMonitorInstanceCounter));  // Allow time for any new runner(s) to start executing
                    }
                    else
                    {
                        // Mutex was not acquired
                    }
                }

                Thread.Sleep(1000);
            }

            // Cancel was requested
            Log.Information($"MapDbPublishJobMonitor.JobMonigtorThreadMain() stopping {ActivePublicationRunnerItems.Count} active JobRunners, waiting up to {StopWaitTimeSeconds}");

            if (ActivePublicationRunnerItems.Count != 0)
            {
                ActivePublicationRunnerItems.ForEach(t => t.tokenSource.Cancel());

                DateTime WaitStart = DateTime.Now;
                while (DateTime.Now - WaitStart < StopWaitTimeSeconds)
                {
                    Log.Information($"MapDbPublishJobMonitor.JobMonigtorThreadMain() waiting for {ActivePublicationRunnerItems.Count} running tasks to complete");

                    int CompletedTaskIndex = Task.WaitAny(ActivePublicationRunnerItems.Select(t => t.task).ToArray(), new TimeSpan(StopWaitTimeSeconds.Ticks / 100));
                    if (CompletedTaskIndex > -1)
                    {
                        ActivePublicationRunnerItems.RemoveAt(CompletedTaskIndex);
                    }

                    if (ActivePublicationRunnerItems.Count == 0)
                    {
                        Log.Information($"MapDbPublishJobMonitor.JobMonigtorThreadMain() all publication runners terminated successfully");
                        break;
                    }
                }

                foreach (var Item in ActivePublicationRunnerItems)
                {
                    Log.Information($"MapDbPublishJobMonitor.JobMonigtorThreadMain() after timer expired, task {Item.requestId.ToString()} not completed");
                }
            }

            Token.ThrowIfCancellationRequested();
            Log.Information($"MapDbPublishJobMonitor.JobMonigtorThreadMain() returning");
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

            using (ApplicationDbContext Db = IsTestMode
                                             ? MockContext.Object
                                             : new ApplicationDbContext(ContextOptions))
            using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
            {
                try
                {
                    IQueryable<ContentPublicationRequest> QueuedPublicationQuery = Db.ContentPublicationRequest.Where(r => DateTime.UtcNow - r.CreateDateTimeUtc > TaskAgeBeforeExecution)
                                                                                                               .Where(r => r.RequestStatus == PublicationStatus.Queued)
                                                                                                               .Include(r => r.RootContentItem)
                                                                                                               .OrderBy(r => r.CreateDateTimeUtc);

                    // Customize the query based on this job monitor type
                    switch (JobMonitorType)
                    {
                        case MapDbPublishJobMonitorType.NonReducingPublications:
                            QueuedPublicationQuery = QueuedPublicationQuery.Where(r => !r.RootContentItem.DoesReduce);
                            break;

                        case MapDbPublishJobMonitorType.ReducingPublications:
                            DateTime EarliestReductionTaskTimestamp = Db.ContentReductionTask
                                .Where(t => new ReductionStatusEnum[] { ReductionStatusEnum.Queued, ReductionStatusEnum.Reducing }.Contains(t.ReductionStatus))
                                .OrderBy(t => t.CreateDateTimeUtc)
                                .Select(t => t.CreateDateTimeUtc)
                                .FirstOrDefault();

                            if (EarliestReductionTaskTimestamp == default(DateTime))  // if no tasks are queued or processing
                            {
                                EarliestReductionTaskTimestamp = DateTime.MaxValue;  // DateTime.MaxValue does not exceed max value of a timestamp in PostgreSQL (so this is ok)
                            }

                            QueuedPublicationQuery = QueuedPublicationQuery.Where(r => r.RootContentItem.DoesReduce)
                                                                           .Where(r => r.CreateDateTimeUtc < EarliestReductionTaskTimestamp);
                            break;

                        default:
                            throw new ApplicationException($"Cannot query publication job queue using unsupported JobMonitorType {JobMonitorType.ToString()}");
                    }

                    List<ContentPublicationRequest> TopItems = QueuedPublicationQuery.Take(ReturnNoMoreThan).ToList();

                    if (TopItems.Count > 0)
                    {
                        TopItems.ForEach(r =>
                        {
                            r.RequestStatus = PublicationStatus.Processing;
                            Log.Information($"PublishJobMonitor({JobMonitorType.ToString()}) initiating processing for PublicationRequestId {r.Id.ToString()}");
                        });
                        Db.SaveChanges();
                        Transaction.Commit();
                    }

                    return TopItems;
                }
                catch (Exception e)
                {
                    Log.Information(GlobalFunctions.LoggableExceptionString(e, $"Failed to query MAP database for available tasks:"));
                    return new List<ContentPublicationRequest>();
                }
            }
        }

        /// <summary>
        /// The request argument should already have its status set to Processing
        /// </summary>
        /// <param name="DbRequest">RequestStatus should already be Processing</param>
        private void LaunchPublishRunnerForRequest(ContentPublicationRequest DbRequest, bool SkipReductionTaskQueueing = false)
        {
            #region Validation
            using (ApplicationDbContext Db = IsTestMode
                                             ? MockContext.Object
                                             : new ApplicationDbContext(ContextOptions))
            {
                var requestInEfCache = Db.ContentPublicationRequest.Find(DbRequest.Id);
                if (requestInEfCache == null)
                {
                    Log.Information($"LaunchPublishRunnerForRequest() could not find a ContentPublicationRequest with Id {DbRequest.Id}");
                    return;
                }
                if (requestInEfCache.RequestStatus != PublicationStatus.Processing)
                {
                    string msg = $"LaunchPublishRunnerForRequest() called for publication request {DbRequest.Id} with unexpected status {ContentPublicationRequest.PublicationStatusString[requestInEfCache.RequestStatus]} while attempting LaunchPublishRunnerForRequest()";
                    Log.Information(msg);
                    Db.ContentPublicationRequest.Update(DbRequest);
                    DbRequest.StatusMessage = msg;
                    DbRequest.RequestStatus = PublicationStatus.Error;
                    Db.SaveChanges();
                    return;
                }
            }
            #endregion

            Task<PublishJobDetail> NewTask = null;
            CancellationTokenSource cancelSource = new CancellationTokenSource();

            // Do I need a switch on ContentType?  (example in MapDbReductionJobMonitor)
            MapDbPublishRunner Runner;
            using (ApplicationDbContext Db = IsTestMode
                                             ? MockContext.Object
                                             : new ApplicationDbContext(ContextOptions))
            {
                Runner = new MapDbPublishRunner
                {
                    JobDetail = PublishJobDetail.New(DbRequest, SkipReductionTaskQueueing),
                };
            }
            if (IsTestMode)
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

        /// <summary>
        /// Updates the MAP database ContentPublicationRequest record with the outcome of <...>PublishRunner processing.
        /// </summary>
        /// <param name="Result">Contains the field values to be saved. All field values will be saved.</param>
        /// <returns></returns>
        private bool UpdateRequest(PublishJobDetail JobDetail)
        {
            if (JobDetail == null || JobDetail.Result == null || JobDetail.JobId == Guid.Empty)
            {
                string Msg = $"MapDbPublishJobMonitor.UpdateRequest unusable argument";
                Log.Information(Msg);
                return false;
            }

            try
            {
                // Use a transaction so that there is no concurrency issue after we get the current db record
                using (ApplicationDbContext Db = IsTestMode
                                                 ? MockContext.Object
                                                 : new ApplicationDbContext(ContextOptions))
                using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
                {
                    ContentPublicationRequest DbRequest = Db.ContentPublicationRequest.Find(JobDetail.JobId);

                    if (DbRequest == null)
                    {
                        return false;
                    }

                    List<ContentReductionTask> RelatedTasks = Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == DbRequest.Id).ToList();

                    // Canceled here implies that the application does not want the update
                    if (DbRequest.RequestStatus == PublicationStatus.Canceled)
                    {
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
                        case PublishJobDetail.JobStatusEnum.Canceled:
                            DbRequest.RequestStatus = PublicationStatus.Canceled;
                            break;
                        case PublishJobDetail.JobStatusEnum.Success:
                            DbRequest.RequestStatus = PublicationStatus.PostProcessReady;
                            DbRequest.ReductionRelatedFilesObj = new List<ReductionRelatedFiles>
                            {
                                new ReductionRelatedFiles
                                {
                                    MasterContentFile = JobDetail.Request.MasterContentFile,
                                    ReducedContentFileList = JobDetail.Result.ResultingRelatedFiles,
                                },
                            };
                            break;
                        default:
                            Log.Information("Unsupported job result status in MapDbPublishJobMonitor.UpdateTask().");
                            return false;
                    }

                    DbRequest.OutcomeMetadataObj = new PublicationRequestOutcomeMetadata
                    {
                        Id = JobDetail.JobId,
                        StartDateTime = JobDetail.Result.StartDateTime,
                        ElapsedTime = JobDetail.Result.ElapsedTime,
                        UserMessage = DbRequest.RequestStatus.GetDisplayDescriptionString(),
                        SupportMessage = JobDetail.Result.StatusMessage,

                        ReductionTaskFailOutcomeList = JobDetail.Result.ReductionTaskFailList,
                        ReductionTaskSuccessOutcomeList = JobDetail.Result.ReductionTaskSuccessList,
                    };

                    DbRequest.StatusMessage = JobDetail.Result.StatusMessage;

                    Db.SaveChanges();
                    Transaction.Commit();
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Information(GlobalFunctions.LoggableExceptionString(e, "Failed to update ContentPublishRequest in database:", true, true));
                return false;
            }
        }

        /// <summary>
        /// Attempts to recover any publication request that is orphaned due to previous termination of this application while the request processing was underway
        /// </summary>
        public override void CleanupOnStart()
        {
            int maxRetries = Configuration.ApplicationConfiguration.GetValue("MaxPublicationRetries", 2);
            const string retryStatusMessagePrefix = "Retry: ";

            lock(_CleanupOnStartLockObj)  // The lock object is declared / initialized in the base class
            {
                using (ApplicationDbContext Db = IsTestMode
                                                 ? MockContext.Object
                                                 : new ApplicationDbContext(ContextOptions))
                {
                    List<ContentPublicationRequest> inProgressPublicationRequests = default;

                    switch (JobMonitorType)
                    {
                        case MapDbPublishJobMonitorType.NonReducingPublications:
                            inProgressPublicationRequests = Db.ContentPublicationRequest
                                                              .Where(r => r.RequestStatus == PublicationStatus.Processing)
                                                              .Where(r => !r.RootContentItem.DoesReduce)
                                                              .ToList();

                            Log.Information($"CleanupOnStart(), job monitor type {JobMonitorType.ToString()}, found {inProgressPublicationRequests.Count} publication requests in progress");

                            foreach (var request in inProgressPublicationRequests)
                            {
                                // Don't retry forever
                                int nextRetry = !string.IsNullOrWhiteSpace(request.StatusMessage) && request.StatusMessage.StartsWith(retryStatusMessagePrefix)
                                    ? int.Parse(request.StatusMessage.Replace(retryStatusMessagePrefix, "")) + 1
                                    : 1;

                                if (nextRetry > maxRetries)
                                {
                                    request.StatusMessage = $"This publication request has exceeded the retry limit of {maxRetries}";
                                    request.RequestStatus = PublicationStatus.Error;

                                    Log.Information($"CleanupOnStart(), job monitor type {JobMonitorType.ToString()}, publication request {request.Id} has exceeded the max retry limit, setting Error status");
                                }
                                else
                                {
                                    request.StatusMessage = $"{retryStatusMessagePrefix}{nextRetry}";
                                    request.RequestStatus = PublicationStatus.Queued;

                                    Log.Information($"CleanupOnStart(), job monitor type {JobMonitorType.ToString()}, publication request {request.Id} will be retried, setting Queued status");
                                }
                                Db.SaveChanges();
                            }
                            break;

                        case MapDbPublishJobMonitorType.ReducingPublications:
                            inProgressPublicationRequests = Db.ContentPublicationRequest
                                                              .Include(r => r.RootContentItem)
                                                              .Where(r => r.RequestStatus == PublicationStatus.Processing)
                                                              .Where(r => r.RootContentItem.DoesReduce)
                                                              .ToList();

                            Log.Information($"CleanupOnStart(), job monitor type {JobMonitorType.ToString()}, found {inProgressPublicationRequests.Count} publication requests in progress");

                            foreach (ContentPublicationRequest request in inProgressPublicationRequests)
                            {
                                // Don't retry forever
                                int nextRetry = !string.IsNullOrWhiteSpace(request.StatusMessage) && request.StatusMessage.StartsWith(retryStatusMessagePrefix)
                                    ? int.Parse(request.StatusMessage.Replace(retryStatusMessagePrefix, "")) + 1
                                    : 1;

                                if (nextRetry > maxRetries)
                                {
                                    Log.Information($"CleanupOnStart() publication request {request.Id} exceeded max retries limit, setting status to PublicationStatus.Error, in job monitor type {JobMonitorType.ToString()}");
                                    request.StatusMessage = $"This publication request has exceeded the retry limit of {maxRetries}";
                                    request.RequestStatus = PublicationStatus.Error;
                                    continue;
                                }

                                
                                // if the master hierarchy has not been extracted yet then just requeue this publication request and cancel all related reduction task records
                                if (!Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == request.Id)
                                                            .Where(t => t.TaskAction == TaskActionEnum.HierarchyOnly)
                                                            .Any(t => t.ReductionStatus == ReductionStatusEnum.Reduced))
                                {
                                    request.RequestStatus = PublicationStatus.Queued;

                                    var allRelatedTasks = Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == request.Id).ToList();
                                    allRelatedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);

                                    Log.Information($"CleanupOnStart() is re-queueing publication request {request.Id}, in job monitor type {JobMonitorType.ToString()}, because the master hierarchy has not been extracted");
                                    Db.SaveChanges();
                                    continue;
                                }

                                var relatedInProgressReductionTasks = Db.ContentReductionTask
                                                                        .Where(t => t.ReductionStatus == ReductionStatusEnum.Reducing)
                                                                        .Where(t => t.ContentPublicationRequestId.Value == request.Id)
                                                                        .ToList();
                                if (relatedInProgressReductionTasks.Any())
                                {
                                    foreach (ContentReductionTask task in relatedInProgressReductionTasks)
                                    {
                                        task.ReductionStatus = ReductionStatusEnum.Queued;
                                        Log.Information($"CleanupOnStart() is re-queueing reduction task {task.Id} for publication request {request.Id}, in job monitor type {JobMonitorType.ToString()}");
                                    }
                                    
                                    Db.SaveChanges();
                                }

                                LaunchPublishRunnerForRequest(request, SkipReductionTaskQueueing: true);
                            }
                            break;

                        default:
                            throw new NotSupportedException($"CleanupOnStart tried to run but unsupported JobMonitorType {JobMonitorType.ToString()}");  // can only happen if the enum gets a new value; in that case we must handle it here
                    }

                    // any request with Queued status or earlier, or PostProcessReady or later, needs no recovery here

                }
            }
        }
    }
}
