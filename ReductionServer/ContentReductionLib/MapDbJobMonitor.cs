/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Intended that one instance of this class monitors a MAP application database for reduction jobs to perform
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using ContentReductionLib.ReductionRunners;
using Newtonsoft.Json;

namespace ContentReductionLib
{
    internal class MapDbJobMonitor : JobMonitorBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private List<(Task task,CancellationTokenSource tokenSource)> ActiveReductionRunnerItems = new List<(Task, CancellationTokenSource)>();

        // Settable operating parameters
        // TODO These should come from configuration.
        internal TimeSpan TaskAgeBeforeExecution { set; private get; }
        private TimeSpan StopWaitTimeSeconds
        {
            get
            {
                if (!int.TryParse(Configuration.ApplicationConfiguration["StopWaitTimeSeconds"], out int WaitSec))
                {
                    WaitSec = 3 * 60;
                }
                return TimeSpan.FromSeconds(WaitSec);
            }
        }

        internal int MaxParallelTasks { set; private get; }

        /// <summary>
        /// ctor, initializes operational parameters to default values
        /// </summary>
        internal MapDbJobMonitor()
        {
            MaxParallelTasks = 1;
            TaskAgeBeforeExecution = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Initializes data used to construct database context instances using a named configuration parameter.
        /// </summary>
        internal string ConfiguredConnectionStringParamName {
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
            }
        }

        /// <summary>
        /// Starts the worker thread of this object
        /// </summary>
        /// <param name="Token">Allows the worker to react to task cancellation by the caller</param>
        /// <returns></returns>
        internal override Task Start(CancellationToken Token)
        {
            if (ContextOptions == null)
            {
                throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
            }

            return Task.Run(() => JobMonitorThreadMain(Token));
        }

        /// <summary>
        /// Main long running thread of the job monitor
        /// </summary>
        /// <param name="Token"></param>
        internal override void JobMonitorThreadMain(CancellationToken Token)
        {
            MethodBase Method = MethodBase.GetCurrentMethod();
            while (!Token.IsCancellationRequested)
            {
                // Remove completed tasks from the RunningTasks collection. 
                foreach ((Task<ReductionJobDetail> task, CancellationTokenSource tokenSource) CompletedReductionRunnerItem in ActiveReductionRunnerItems.Where(t => t.task.IsCompleted).ToList())
                {
                    UpdateTask(CompletedReductionRunnerItem.task.Result);
                    ActiveReductionRunnerItems.Remove(CompletedReductionRunnerItem);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (ActiveReductionRunnerItems.Count < MaxParallelTasks)
                {
                    List<ContentReductionTask> Responses = GetReadyTasks(MaxParallelTasks - ActiveReductionRunnerItems.Count);

                    foreach (ContentReductionTask T in Responses)
                    {
                        Task NewTask = null;
                        CancellationTokenSource cancelSource = new CancellationTokenSource();

                        switch (T.SelectionGroup.RootContentItem.ContentType.TypeEnum)
                        {
                            case ContentTypeEnum.Qlikview:
                                QvReductionRunner Runner = new QvReductionRunner
                                {
                                    JobDetail = (ReductionJobDetail)T,
                                };

                                NewTask = Task.Run(() => Runner.Execute(cancelSource.Token));
                                break;

                            default:
                                Trace.WriteLine($"Task record discovered for unsupported content type");
                                break;
                        }

                        if (NewTask != null)
                        {
                            ActiveReductionRunnerItems.Add( (NewTask, cancelSource) );
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            if (ActiveReductionRunnerItems.Count == 0)
            {
                Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} stopped due to cancellation request");
                return;
            }

            Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} received stop request, cancelling {ActiveReductionRunnerItems.Count} active JobRunners, waiting up to {StopWaitTimeSeconds}");
            ActiveReductionRunnerItems.ForEach(t => t.tokenSource.Cancel());

            DateTime WaitStart = DateTime.Now;
            while (DateTime.Now - WaitStart < StopWaitTimeSeconds)
            {
                Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} waiting for {ActiveReductionRunnerItems.Count} running tasks to complete");

                int CompletedTaskIndex = Task.WaitAny(ActiveReductionRunnerItems.Select(t => t.task).ToArray(), new TimeSpan(StopWaitTimeSeconds.Ticks/100));
                if (CompletedTaskIndex > -1)
                {
                    ActiveReductionRunnerItems.RemoveAt(CompletedTaskIndex);
                }

                if (ActiveReductionRunnerItems.Count == 0)
                {
                    Trace.WriteLine($"{DateTime.Now} {Method.ReflectedType.Name}.{Method.Name} all reduction runners terminated successfully");
                    return;
                }
            }

            Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} after timer expired, {ActiveReductionRunnerItems.Count} reduction tasks not completed");
        }

        /// <summary>
        /// Query the database for tasks to be run
        /// </summary>
        /// <param name="ReturnNoMoreThan">The maximum number of records to return</param>
        /// <returns></returns>
        internal List<ContentReductionTask> GetReadyTasks(int ReturnNoMoreThan)
        {
            if (ReturnNoMoreThan < 1)
            {
                return new List<ContentReductionTask>();
            }

            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
            {
                List<ContentReductionTask> TopItems = new List<ContentReductionTask>();
                try
                {
                    TopItems.AddRange(Db.ContentReductionTask.Where(t => DateTimeOffset.UtcNow - t.CreateDateTime > TaskAgeBeforeExecution)
                                                             .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued)
                                                             .Include(t => t.SelectionGroup).ThenInclude(sg => sg.RootContentItem).ThenInclude(rc => rc.ContentType)
                                                             .OrderBy(t => t.CreateDateTime)
                                                             .Take(ReturnNoMoreThan)
                                                             .ToList());
                    TopItems.ForEach(rt => rt.ReductionStatus = ReductionStatusEnum.Reducing);
                    Db.ContentReductionTask.UpdateRange(TopItems);
                    Db.SaveChanges();
                    Transaction.Commit();
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Failed to query MAP database for available tasks.  Exception:{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                    throw;
                }

                return TopItems;
            }
        }

        /// <summary>
        /// Updates the MAP database ContentReductionTask record with the outcome of <...>ReductionRunner processing.
        /// </summary>
        /// <param name="Result">Contains the field values to be saved. All field values will be saved.</param>
        /// <returns></returns>
        private bool UpdateTask(ReductionJobDetail JobDetail)
        {
            if (JobDetail == null || JobDetail.Result == null || JobDetail.TaskId == Guid.Empty)
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
                    ContentReductionTask DbTask = Db.ContentReductionTask.Find(JobDetail.TaskId);

                    // Canceled here implies that the application does not want the update
                    if (DbTask == null || DbTask.ReductionStatus == ReductionStatusEnum.Canceled)
                    {
                        return false;
                    }

                    switch (JobDetail.Result.Status)
                    {
                        case JobStatusEnum.Unspecified:
                            DbTask.ReductionStatus = ReductionStatusEnum.Unspecified;
                            break;
                        case JobStatusEnum.Error:
                            DbTask.ReductionStatus = ReductionStatusEnum.Error;
                            break;
                        case JobStatusEnum.Success:
                            DbTask.ReductionStatus = ReductionStatusEnum.Reduced;
                            break;
                        case JobStatusEnum.Canceled:
                            DbTask.ReductionStatus = ReductionStatusEnum.Canceled;
                            break;
                        default:
                            throw new Exception("Unsupported job result status in MapDbJobMonitor.UpdateTask().");
                    }

                    DbTask.MasterContentHierarchy = JobDetail.Result.MasterContentHierarchy != null 
                                                ? JsonConvert.SerializeObject((ContentReductionHierarchy<ReductionFieldValue>)JobDetail.Result.MasterContentHierarchy, Formatting.Indented)
                                                : null;

                    DbTask.ReducedContentHierarchy = JobDetail.Result.ReducedContentHierarchy != null
                                                ? JsonConvert.SerializeObject((ContentReductionHierarchy<ReductionFieldValue>)JobDetail.Result.ReducedContentHierarchy, Formatting.Indented)
                                                : null;

                    DbTask.ResultFilePath = JobDetail.Result.ReducedContentFilePath;

                    DbTask.ReducedContentChecksum = JobDetail.Result.ReducedContentFileChecksum;

                    DbTask.ReductionStatusMessage = JobDetail.Result.StatusMessage;

                    Db.ContentReductionTask.Update(DbTask);
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
