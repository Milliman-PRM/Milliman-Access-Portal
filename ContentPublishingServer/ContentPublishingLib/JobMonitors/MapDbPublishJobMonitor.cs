/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using ContentPublishingLib.JobRunners;
using TestResourcesLib;
using Newtonsoft.Json;

namespace ContentPublishingLib.JobMonitors
{
    internal class MapDbPublishJobMonitor : JobMonitorBase
    {
        internal class PublishJobTrackingItem
        {
            internal Task<PublishJobDetail> task;
            internal CancellationTokenSource tokenSource;
            internal ContentPublicationRequest dbRequest;
        }

        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private List<PublishJobTrackingItem> ActivePublicationRunnerItems = new List<PublishJobTrackingItem>();

        private int MaxParallelRequests
        {
            get
            {
                try
                {
                    if (int.TryParse(Configuration.ApplicationConfiguration["MaxParallelTasks"], out int MaxTasks))
                    {
                        return MaxTasks;
                    }
                }
                catch
                { }
                return 1;
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
            if (ContextOptions == null)
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

                        // TODO Do I need a switch on ContentType?  (example in MapDbReductionJobMonitor)
                        MapDbPublishRunner Runner;
                        using (ApplicationDbContext Db = MockContext != null
                                                         ? MockContext.Object
                                                         : new ApplicationDbContext(ContextOptions))
                        {
                            Runner = new MapDbPublishRunner
                            {
                                JobDetail = PublishJobDetail.New(DbRequest, Db),
                                ConnectionString = ConnectionString,
                            };
                        }
                        if (MockContext != null)
                        {
                            Runner.SetTestAuditLogger(MockAuditLogger.New().Object);
                        }

                        NewTask = Task.Run(() => Runner.Execute(cancelSource.Token), cancelSource.Token);

                        if (NewTask != null)
                        {
                            ActivePublicationRunnerItems.Add(new PublishJobTrackingItem { dbRequest = DbRequest, task = NewTask, tokenSource = cancelSource });
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} stopping {ActivePublicationRunnerItems.Count} active JobRunners, waiting up to {StopWaitTimeSeconds}");

            if (ActivePublicationRunnerItems.Count != 0)
            {
                ActivePublicationRunnerItems.ForEach(t => t.tokenSource.Cancel());

                DateTime WaitStart = DateTime.Now;
                while (DateTime.Now - WaitStart < StopWaitTimeSeconds)
                {
                    Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} waiting for {ActivePublicationRunnerItems.Count} running tasks to complete");

                    int CompletedTaskIndex = Task.WaitAny(ActivePublicationRunnerItems.Select(t => t.task).ToArray(), new TimeSpan(StopWaitTimeSeconds.Ticks / 100));
                    if (CompletedTaskIndex > -1)
                    {
                        ActivePublicationRunnerItems.RemoveAt(CompletedTaskIndex);
                    }

                    if (ActivePublicationRunnerItems.Count == 0)
                    {
                        Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} all publication runners terminated successfully");
                        break;
                    }
                }

                foreach (var Item in ActivePublicationRunnerItems)
                {
                    Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} after timer expired, task {Item.dbRequest.Id.ToString()} not completed");
                }
            }

            Token.ThrowIfCancellationRequested();
            Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} returning");
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
                    List<ContentPublicationRequest> TopItems = Db.ContentPublicationRequest.Where(r => r.RequestStatus == PublicationStatus.Queued)
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
                    Trace.WriteLine($"Failed to query MAP database for available tasks.  Exception:{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                    throw;
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
            if (JobDetail == null || JobDetail.Result == null || JobDetail.JobId == -1)
            {
                MethodBase Method = MethodBase.GetCurrentMethod();
                string Msg = $"{Method.ReflectedType.Name}.{Method.Name} unusable argument";
                Trace.WriteLine(Msg);
                return false;
            }

            try
            {
                // Use a transaction so that there is no concurrency issue after we get the current db record
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
                {
                    ContentPublicationRequest DbRequest = Db.ContentPublicationRequest.Find(JobDetail.JobId);

                    // Canceled here implies that the application does not want the update
                    if (DbRequest == null || DbRequest.RequestStatus == PublicationStatus.Canceled)
                    {
                        return false;
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
                            DbRequest.RequestStatus = PublicationStatus.Complete;
                            break;
                        case PublishJobDetail.JobStatusEnum.Canceled:
                            DbRequest.RequestStatus = PublicationStatus.Canceled;
                            break;
                        default:
                            throw new Exception("Unsupported job result status in MapDbJobMonitor.UpdateTask().");
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
                Trace.WriteLine("Failed to update task in database" + Environment.NewLine + e.Message);
                return false;
            }
        }
    }
}
