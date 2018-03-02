/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
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
                // TODO clean up RunningTasks collection if any are completed. 

                // Start more tasks if there is room in the RunningTasks collection
                if (RunningTasks.Count < MaxParallelTasks)
                {
                    List<ContentReductionTask> Responses = GetReadyTasks(MaxParallelTasks - RunningTasks.Count);

                    // TODO Correct this
                    RunningTasks.AddRange(Responses);

                    Trace.WriteLine($"GetReadyTasks iteration {LoopCount++} completed with response item Ids: {string.Join(",", Responses.Select(rt => rt.Id))}");
                }
                Thread.Sleep(4000);
            }
            Trace.WriteLine("JobMonitorThreadMain terminating due to cancellation");
        }

        internal List<ContentReductionTask> GetReadyTasks(int MaxCount)
        {
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            using (IDbContextTransaction Transaction = Db.Database.BeginTransaction())
            {
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => t.CreateDateTime - DateTimeOffset.UtcNow < TimeSpan.FromSeconds(30))
                                                                                .OrderBy(t => t.CreateDateTime)
                                                                                .Take(MaxCount)
                                                                                .ToList();
                TopItems.ForEach(rt => rt.Status = "Processing");
                Db.ContentReductionTask.UpdateRange(TopItems);
                Db.SaveChanges();
                Transaction.Commit();

                return TopItems;
            }
        }

    }
}
