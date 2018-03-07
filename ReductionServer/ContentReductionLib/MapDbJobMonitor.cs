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

        internal MapDbJobMonitor()
        {
            MaxParallelTasks = 1;
        }

        internal int MaxParallelTasks { get; set; }

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

        internal string ConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                ContextOptions = ContextBuilder.Options;
            }
        }

        internal override Task Start(CancellationToken Token)
        {
            if (ContextOptions == null)
            {
                throw new NullReferenceException("Attempting to construct new ApplicationDbContext but connection string not initialized");
            }

            return Task.Run(() => JobMonitorThreadMain(Token));
        }

        internal override void JobMonitorThreadMain(CancellationToken Token)
        {
            int LoopCount = 0;
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
                            Guid G = T.Id;  // disposable statement
                        });

                        RunningTasks.Add(task);
                    }

                    Trace.WriteLine($"GetReadyTasks iteration {LoopCount++} completed with response item Ids: {string.Join(",", Responses.Select(rt => rt.Id))}");
                }
                Thread.Sleep(1000);
            }
            Trace.WriteLine("JobMonitorThreadMain terminating due to cancellation");
        }

        internal List<ContentReductionTask> GetReadyTasks(int MaxCount)
        {
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
            {
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => t.CreateDateTime - DateTimeOffset.UtcNow < TimeSpan.FromSeconds(30))
                                                                             .Where(t => t.ReductionStatus == ReductionStatusEnum.Queued)
                                                                             .OrderBy(t => t.CreateDateTime)
                                                                             .Take(MaxCount)
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
