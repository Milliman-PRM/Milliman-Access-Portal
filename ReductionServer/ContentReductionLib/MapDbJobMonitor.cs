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
        private List<(Task task,CancellationTokenSource tokenSource)> RunningTasks = new List<(Task, CancellationTokenSource)>();

        // Settable operating parameters
        // TODO These should come from configuration.
        internal TimeSpan TaskAgeBeforeExecution { set; private get; }
        private TimeSpan WaitTimeForTasksOnStop { get { return TimeSpan.FromMinutes(3); } }

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
                ConfigurationBuilder CfgBuilder = new ConfigurationBuilder();
                // TODO add something for AzureKeyVault in CI and production environments
                CfgBuilder.AddUserSecrets<MapDbJobMonitor>()
                            .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot MyConfig = CfgBuilder.Build();

                ConnectionString = MyConfig.GetConnectionString(value);
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
            while (!Token.IsCancellationRequested)
            {
                // Remove completed tasks from the RunningTasks collection. 
                foreach ((Task<ReductionJobResult> task, CancellationTokenSource tokenSource) CompletedItem in RunningTasks.Where(t => t.task.IsCompleted).ToList())
                {
                    UpdateTask(CompletedItem.task.Result);
                    RunningTasks.Remove(CompletedItem);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (RunningTasks.Count < MaxParallelTasks)
                {
                    List<ContentReductionTask> Responses = GetReadyTasks(MaxParallelTasks - RunningTasks.Count);

                    foreach (ContentReductionTask T in Responses)
                    {
                        Task NewTask = null;
                        CancellationTokenSource cancelSource = new CancellationTokenSource();

                        switch (T.SelectionGroup.RootContentItem.ContentType.TypeEnum)
                        {
                            case ContentTypeEnum.Qlikview:
                                QvReductionRunner Runner = new QvReductionRunner
                                {
                                    QueueTask = T,
                                };
                                NewTask = Task.Run(() => Runner.ExecuteReduction(cancelSource.Token));
                                break;

                            default:
                                NewTask = Task.Run(() =>
                                {
                                    // All of the below simulates what will be done in the reduction related class
                                    for (int i = 0; i < 5; i++)
                                    {
                                        Thread.Sleep(1000);  // doing some reduction work
                                        Trace.WriteLine($"Dummy task for unsupported content type on iteration {i}");
                                    }
                                });
                                break;
                        }

                        if (NewTask != null)
                        {
                            RunningTasks.Add( (NewTask, cancelSource) );
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            MethodBase Method = MethodBase.GetCurrentMethod();
            Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} received stop request, waiting up to {WaitTimeForTasksOnStop} for any running tasks to complete");
            DateTime WaitStart = DateTime.Now;

            while (RunningTasks.Count > 0)
            {
                if (DateTime.Now - WaitStart > WaitTimeForTasksOnStop)
                {
                    break;
                }

                RunningTasks.ForEach(t => t.tokenSource.Cancel());

                int CompletedTaskIndex = Task.WaitAny(RunningTasks.Select(t => t.task).ToArray(), new TimeSpan(WaitTimeForTasksOnStop.Ticks/100));
                if (CompletedTaskIndex > -1)
                {
                    RunningTasks.RemoveAt(CompletedTaskIndex);
                }
            }
            Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} after timer expired, {RunningTasks.Count} reduction tasks not completed");
            Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} stopped due to application request");
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
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => DateTimeOffset.UtcNow - t.CreateDateTime > TaskAgeBeforeExecution)
                                                                             .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued)
                                                                             .Include(t => t.SelectionGroup).ThenInclude(sg => sg.RootContentItem).ThenInclude(rc => rc.ContentType)
                                                                             .OrderBy(t => t.CreateDateTime)
                                                                             .Take(ReturnNoMoreThan)
                                                                             .ToList();
                TopItems.ForEach(rt => rt.ReductionStatus = ReductionStatusEnum.Reducing);
                Db.ContentReductionTask.UpdateRange(TopItems);
                Db.SaveChanges();
                Transaction.Commit();

                return TopItems;
            }
        }

        private bool UpdateTask(ReductionJobResult Result)
        {
            try
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    ContentReductionTask DbTask = Db.ContentReductionTask.Find(Result.TaskId);

                    if (DbTask == null || DbTask.ReductionStatus == ReductionStatusEnum.Canceled)
                    {
                        return false;
                    }

                    switch (Result.Status)
                    {
                        case ReductionJobStatusEnum.Unspecified:
                            break;
                        case ReductionJobStatusEnum.Error:
                            DbTask.ReductionStatus = ReductionStatusEnum.Error;
                            break;
                        case ReductionJobStatusEnum.Success:
                            DbTask.ReductionStatus = ReductionStatusEnum.Reduced;
                            break;
                        default:
                            throw new Exception("Unsupported job result status in MapDbJobMonitor.UpdateTask().");
                    }

                    DbTask.ExtractedHierarchy = JsonConvert.SerializeObject((ContentReductionHierarchy<ReductionFieldValue>)Result.ExtractedHierarchy, Formatting.Indented);

                    DbTask.ResultFilePath = Result.ReducedContentFilePath;

                    Db.ContentReductionTask.Update(DbTask);
                    Db.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
