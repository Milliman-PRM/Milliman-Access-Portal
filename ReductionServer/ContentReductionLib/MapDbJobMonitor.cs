/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Intended that one instance of this class monitors a MAP application database for reductino jobs to perform
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using MapDbContextLib.Context;

namespace ContentReductionLib
{
    internal class MapDbJobMonitor : JobMonitorBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private List<Task> RunningTasks = new List<Task>();

        /// <summary>
        /// ctor, initializes operational parameters
        /// </summary>
        internal MapDbJobMonitor()
        {
            MaxParallelTasks = 1;
        }

        internal int MaxParallelTasks { get; set; }

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
                foreach (Task CompletedTask in RunningTasks.Where(t => t.IsCompleted).ToList())
                {
                    RunningTasks.Remove(CompletedTask);
                }

                // Start more tasks if there is room in the RunningTasks collection. 
                if (RunningTasks.Count < MaxParallelTasks)
                {
                    List<ContentReductionTask> Responses = GetReadyTasks(MaxParallelTasks - RunningTasks.Count);

                    foreach (ContentReductionTask T in Responses)
                    {
                        // Initiate the reduction job

                        var task = Task.Run(() => 
                        {
                            // Start the job running
                            /* Create/configureLaunch an instance of ReductionRunner */

                            // All of the below simulates what will be done in the reduction related class
                            Guid G = T.Id;  // disposable statement
                            for (int i = 0; i<5; i++)
                            {
                                Thread.Sleep(1000);  // doing some reduction work
                                Trace.WriteLine($"Task {G.ToString()} on iteration {i}");
                            }
                            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                            {
                                Db.ContentReductionTask.Find(G).ReductionStatus = ReductionStatusEnum.Reduced;
                                Db.SaveChanges();
                            }

                        });

                        RunningTasks.Add(task);
                    }

                    //Trace.WriteLine($"GetReadyTasks iteration {LoopCount++} completed with response item Ids: {string.Join(",", Responses.Select(rt => rt.Id))}");
                }
                Thread.Sleep(1000);
            }
            Trace.WriteLine("JobMonitorThreadMain terminating due to cancellation");
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
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => t.CreateDateTime - DateTimeOffset.UtcNow < TimeSpan.FromSeconds(30))
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

    }
}
