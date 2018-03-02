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
using Microsoft.Extensions.Configuration;
using MapDbContextLib.Context;

namespace ContentReductionLib
{
    internal class MapDbJobMonitor : JobMonitorBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;

        internal MapDbJobMonitor()
        {}

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
                int ResponseCount = DoWork(4);
                Thread.Sleep(4000);
                Trace.WriteLine($"GetItems iteration {LoopCount++} completed with {ResponseCount} responses");
            }
            Trace.WriteLine("JobMonitorThreadMain terminating due to cancellation");
        }

        internal override int DoWork(int MaxCount)
        {
            using (var Db=new ApplicationDbContext(ContextOptions))
            {
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => t.CreateDateTime - DateTimeOffset.UtcNow < TimeSpan.FromSeconds(30))
                                                                             .OrderBy(t => t.CreateDateTime)
                                                                             .Take(MaxCount)
                                                                             .ToList();

                return TopItems.Count;
            }
        }
    }
}
