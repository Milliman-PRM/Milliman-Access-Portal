/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A queue monitor to initiate processing of appropriate publication requests
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Services;
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
        public MapDbPublishJobMonitor(MapDbPublishJobMonitorType Type, IAuditLogger testAuditLogger = null)
            :base(testAuditLogger)
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

        public SemaphoreSlim QueueSemaphore { private get; set; }

        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private List<PublishJobTrackingItem> ActivePublicationRunnerItems = new List<PublishJobTrackingItem>();

        private TimeSpan TaskAgeBeforeExecution
        {
            get
            {
                int TaskAgeSec = Configuration.ApplicationConfiguration.GetValue("TaskAgeBeforeExecutionSeconds", 30);
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
        public string ConnectionString
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
        public async override Task StartAsync(CancellationToken Token)
        {
            if (ContextOptions == null)
            {
                throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
            }

            await CleanupOnStartAsync();

            await JobMonitorThreadMainAsync(Token);
        }

        public async override Task JobMonitorThreadMainAsync(CancellationToken Token)
        {
            await Task.Yield();

            // Main loop
            while (!Token.IsCancellationRequested)
            {
                // Remove completed tasks from the RunningTasks collection. 
                // .ToList() is needed because the body changes the original List. 
                foreach (PublishJobTrackingItem CompletedPublishRunnerItem in ActivePublicationRunnerItems.Where(t => t.task.IsCompleted).ToList())
                {
                    Log.Information($"PublishJobMonitor({JobMonitorType.ToString()}) completed processing for PublicationRequestId {CompletedPublishRunnerItem.requestId.ToString()}");
                    await UpdateRequestAsync(CompletedPublishRunnerItem.task.Result);
                    ActivePublicationRunnerItems.Remove(CompletedPublishRunnerItem);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (ActivePublicationRunnerItems.Count < MaxConcurrentRunners)
                {
                    if (QueueSemaphore.Wait(new TimeSpan(0, 0, 20)))
                    {
                        // Can't await async code here because the Mutex is owned by the current thread
                        List<ContentPublicationRequest> Responses = await GetReadyRequestsAsync(MaxConcurrentRunners - ActivePublicationRunnerItems.Count);

                        foreach (ContentPublicationRequest DbRequest in Responses)
                        {
                            LaunchPublishRunnerForRequest(DbRequest);
                        }

                        QueueSemaphore.Release();
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
            Log.Information($"MapDbPublishJobMonitor.JobMonitorThreadMain() stopping {ActivePublicationRunnerItems.Count} active JobRunners, waiting up to {StopWaitTimeSeconds}");

            if (ActivePublicationRunnerItems.Count != 0)
            {
                ActivePublicationRunnerItems.ForEach(t => t.tokenSource.Cancel());

                DateTime WaitStart = DateTime.Now;
                while (DateTime.Now - WaitStart < StopWaitTimeSeconds)
                {
                    Log.Information($"MapDbPublishJobMonitor.JobMonitorThreadMain() waiting for {ActivePublicationRunnerItems.Count} running tasks to complete");

                    int CompletedTaskIndex = Task.WaitAny(ActivePublicationRunnerItems.Select(t => t.task).ToArray(), new TimeSpan(StopWaitTimeSeconds.Ticks / 100));
                    if (CompletedTaskIndex > -1)
                    {
                        ActivePublicationRunnerItems.RemoveAt(CompletedTaskIndex);
                    }

                    if (ActivePublicationRunnerItems.Count == 0)
                    {
                        Log.Information($"MapDbPublishJobMonitor.JobMonitorThreadMain() all publication runners terminated successfully");
                        break;
                    }
                }

                foreach (var Item in ActivePublicationRunnerItems)
                {
                    Log.Information($"MapDbPublishJobMonitor.JobMonitorThreadMain() after timer expired, task {Item.requestId.ToString()} not completed");
                }
            }

            Token.ThrowIfCancellationRequested();
            Log.Information($"MapDbPublishJobMonitor.JobMonitorThreadMain() returning");
        }

        /// <summary>
        /// Query the database for tasks to be run
        /// </summary>
        /// <param name="ReturnNoMoreThan">The maximum number of records to return. If <1, an empty list is returned.</param>
        /// <returns></returns>
        internal async Task<List<ContentPublicationRequest>> GetReadyRequestsAsync(int ReturnNoMoreThan)
        {
            if (ReturnNoMoreThan < 1)
            {
                return new List<ContentPublicationRequest>();
            }

            try
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                using (IDbContextTransaction Transaction = await Db.Database.BeginTransactionAsync())
                {
                    IQueryable<ContentPublicationRequest> QueuedPublicationQuery = Db.ContentPublicationRequest.Where(r => r.RequestStatus == PublicationStatus.Queued)
                                                                                                               .Include(r => r.RootContentItem)
                                                                                                                .ThenInclude(rci => rci.ContentType)
                                                                                                               .OrderBy(r => r.CreateDateTimeUtc);

                    // Customize the query based on this job monitor type
                    switch (JobMonitorType)
                    {
                        case MapDbPublishJobMonitorType.NonReducingPublications:
                            DateTime onlyTasksCreatedBeforeUtc = DateTime.UtcNow - TaskAgeBeforeExecution;
                            QueuedPublicationQuery = QueuedPublicationQuery.Where(r => !r.RootContentItem.DoesReduce)
                                                                           .Where(r => r.CreateDateTimeUtc < onlyTasksCreatedBeforeUtc);
                            break;

                        case MapDbPublishJobMonitorType.ReducingPublications:
                            DateTime EarliestReductionTaskTimestamp = await Db.ContentReductionTask
                                .Where(t => new ReductionStatusEnum[] { ReductionStatusEnum.Queued, ReductionStatusEnum.Reducing }.Contains(t.ReductionStatus))
                                .OrderBy(t => t.CreateDateTimeUtc)
                                .Select(t => t.CreateDateTimeUtc)
                                .FirstOrDefaultAsync();

                            if (EarliestReductionTaskTimestamp == default)  // if no tasks are queued or processing
                            {
                                EarliestReductionTaskTimestamp = DateTime.MaxValue;  // DateTime.MaxValue does not exceed max value of a timestamp in PostgreSQL (so this is ok)
                            }

                            QueuedPublicationQuery = QueuedPublicationQuery.Where(r => r.RootContentItem.DoesReduce)
                                                                           .Where(r => r.CreateDateTimeUtc < EarliestReductionTaskTimestamp);
                            break;

                        default:
                            throw new ApplicationException($"Cannot query publication job queue using unsupported JobMonitorType {JobMonitorType.ToString()}");
                    }

                    List<ContentPublicationRequest> TopItems = await QueuedPublicationQuery.Take(ReturnNoMoreThan).ToListAsync();

                    if (TopItems.Count > 0)
                    {
                        TopItems.ForEach(r =>
                        {
                            r.RequestStatus = PublicationStatus.Processing;
                            Log.Information($"PublishJobMonitor({JobMonitorType}) initiating processing for PublicationRequestId {r.Id}");
                        });
                        await Db.SaveChangesAsync();
                        await Transaction.CommitAsync();
                    }

                    return TopItems;
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to query MAP database for available tasks:");
                return new List<ContentPublicationRequest>();
            }
        }

        /// <summary>
        /// The request argument should already have its status set to Processing
        /// </summary>
        /// <param name="DbRequest">RequestStatus should already be Processing</param>
        private void LaunchPublishRunnerForRequest(ContentPublicationRequest DbRequest, bool SkipReductionTaskQueueing = false)
        {
            #region Validation
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                var requestInEfCache = Db.ContentPublicationRequest.Find(DbRequest.Id);
                if (requestInEfCache == null)
                {
                    Log.Error($"PublishJobMonitor({JobMonitorType}).LaunchPublishRunnerForRequest() could not find a ContentPublicationRequest with Id {DbRequest.Id}");
                    return;
                }
                if (requestInEfCache.RequestStatus != PublicationStatus.Processing)
                {
                    string msg = $"PublishJobMonitor({JobMonitorType}).LaunchPublishRunnerForRequest() called for publication request {DbRequest.Id} with unexpected status {ContentPublicationRequest.PublicationStatusString[requestInEfCache.RequestStatus]} while attempting LaunchPublishRunnerForRequest()";
                    Log.Information(msg);
                    requestInEfCache.StatusMessage = msg;
                    requestInEfCache.RequestStatus = PublicationStatus.Error;
                    Db.SaveChanges();
                    return;
                }
            }
            #endregion

            Task<PublishJobDetail> NewTask = null;
            CancellationTokenSource cancelSource = new CancellationTokenSource();

            // Do I need a switch on ContentType?  (example in MapDbReductionJobMonitor)
            MapDbPublishRunner Runner = new MapDbPublishRunner
            {
                JobDetail = PublishJobDetail.New(DbRequest, SkipReductionTaskQueueing),
                ConnectionString = ConnectionString,
            };
            if (TestAuditLogger != null)
            {
                Runner.SetTestAuditLogger(TestAuditLogger);
            }

            NewTask = Runner.Execute(cancelSource.Token);

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
        private async Task<bool> UpdateRequestAsync(PublishJobDetail JobDetail)
        {
            if (JobDetail == null || JobDetail.Result == null || JobDetail.JobId == Guid.Empty)
            {
                Log.Information("PublishJobMonitor({JobMonitorType}).UpdateRequest unusable JobDetail argument");
                return false;
            }

            try
            {
                // Use a transaction so that there is no concurrency issue after we get the current db record
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                using (IDbContextTransaction Transaction = await Db.Database.BeginTransactionAsync())
                {
                    ContentPublicationRequest DbRequest = await Db.ContentPublicationRequest.FindAsync(JobDetail.JobId);

                    if (DbRequest == null)
                    {
                        return false;
                    }

                    List<ContentReductionTask> RelatedTasks = await Db.ContentReductionTask
                                                                      .Where(t => t.ContentPublicationRequestId == DbRequest.Id)
                                                                      .ToListAsync();

                    // Canceled here implies that the application does not want the update
                    if (DbRequest.RequestStatus == PublicationStatus.Canceled)
                    {
                        RelatedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);
                        await Db.SaveChangesAsync();
                        await Transaction.CommitAsync();
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
                            Log.Information($"PublishJobMonitor({JobMonitorType}).UpdateRequestAsync, unsupported job result status {JobDetail.Status}");
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

                    await Db.SaveChangesAsync();
                    await Transaction.CommitAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to update ContentPublishRequest in database:");
                return false;
            }
        }

        /// <summary>
        /// Attempts to recover any publication request that is orphaned due to previous termination of this application while the request processing was underway
        /// </summary>
        public async override Task CleanupOnStartAsync()
        {
            int maxRetries = Configuration.ApplicationConfiguration.GetValue("MaxPublicationRetries", 2);
            const string retryStatusMessagePrefix = "Retry: ";

            bool acquired = await _CleanupOnStartSemaphore.WaitAsync(TimeSpan.FromSeconds(60));
            if (!acquired)
            {
                string msg = $"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart(), failed to acquire semaphore in CleanupOnStart()";
                Log.Error(msg);
                throw new TimeoutException(msg);
            }

            try
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    List<ContentPublicationRequest> inProgressPublicationRequests = default;

                    switch (JobMonitorType)
                    {
                        case MapDbPublishJobMonitorType.NonReducingPublications:
                            inProgressPublicationRequests = await Db.ContentPublicationRequest
                                                                    .Where(r => r.RequestStatus == PublicationStatus.Processing)
                                                                    .Where(r => !r.RootContentItem.DoesReduce)
                                                                    .ToListAsync();
                            break;

                        case MapDbPublishJobMonitorType.ReducingPublications:
                            inProgressPublicationRequests = await Db.ContentPublicationRequest
                                                                    .Include(r => r.RootContentItem)
                                                                    .Where(r => r.RequestStatus == PublicationStatus.Processing)
                                                                    .Where(r => r.RootContentItem.DoesReduce)
                                                                    .ToListAsync();
                            break;

                        default:
                            throw new NotSupportedException($"CleanupOnStart tried to run but unsupported JobMonitorType {JobMonitorType}");  // can only happen if the enum gets a new value; in that case we must handle it here
                    }

                    Log.Information($"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart(), found {inProgressPublicationRequests.Count} publication requests in progress");

                    foreach (var request in inProgressPublicationRequests)
                    {
                        // Don't retry forever
                        int nextRetry = !string.IsNullOrWhiteSpace(request.StatusMessage) && request.StatusMessage.StartsWith(retryStatusMessagePrefix)
                            ? int.Parse(request.StatusMessage.Replace(retryStatusMessagePrefix, "")) + 1
                            : 1;

                        if (nextRetry > maxRetries)
                        {
                            Log.Information($"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart(), publication request {request.Id} has exceeded the max retry limit, setting Error status");
                            request.StatusMessage = $"This publication request has exceeded the retry limit of {maxRetries}";
                            request.RequestStatus = PublicationStatus.Error;
                        }
                        else
                        {
                            switch (JobMonitorType)
                            {
                                case MapDbPublishJobMonitorType.NonReducingPublications:
                                    request.StatusMessage = $"{retryStatusMessagePrefix}{nextRetry}";
                                    request.RequestStatus = PublicationStatus.Queued;

                                    Log.Information($"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart(), publication request {request.Id} will be retried, setting Queued status");
                                    break;

                                case MapDbPublishJobMonitorType.ReducingPublications:
                                    // if the master hierarchy has not been extracted yet then just requeue this publication request and cancel all related reduction task records
                                    if (!await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == request.Id)
                                                                      .Where(t => t.TaskAction == TaskActionEnum.HierarchyOnly)
                                                                      .AnyAsync(t => t.ReductionStatus == ReductionStatusEnum.Reduced))
                                    {
                                        request.RequestStatus = PublicationStatus.Queued;

                                        var allRelatedTasks = await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == request.Id).ToListAsync();
                                        allRelatedTasks.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);

                                        Log.Information($"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart() re-queueing publication request {request.Id}, in job monitor type {JobMonitorType.ToString()}, because the master hierarchy has not been extracted");
                                    }
                                    else
                                    {
                                        var relatedInProgressReductionTasks = await Db.ContentReductionTask
                                                                                      .Where(t => t.ReductionStatus == ReductionStatusEnum.Reducing)
                                                                                      .Where(t => t.ContentPublicationRequestId.Value == request.Id)
                                                                                      .ToListAsync();
                                        if (relatedInProgressReductionTasks.Any())
                                        {
                                            foreach (ContentReductionTask task in relatedInProgressReductionTasks)
                                            {
                                                task.ReductionStatus = ReductionStatusEnum.Queued;
                                                Log.Information($"MapDbPublishJobMonitor({JobMonitorType}).CleanupOnStart() re-queueing reduction task {task.Id} for publication request {request.Id}, in job monitor type {JobMonitorType.ToString()}");
                                            }
                                        }

                                        LaunchPublishRunnerForRequest(request, SkipReductionTaskQueueing: true);
                                    }
                                    break;
                            }
                        }

                        await Db.SaveChangesAsync();
                    }

                }

                // any request with Queued status or earlier, or PostProcessReady or later, needs no recovery here
            }
            finally
            {
                _CleanupOnStartSemaphore.Release();
            }
        }
    }
}
