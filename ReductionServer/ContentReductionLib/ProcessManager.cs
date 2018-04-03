/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Intended as the main library API for use by applications.  
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuditLogLib;

namespace ContentReductionLib
{
    public class ProcessManager
    {
        // This collection is keyed on the config file name
        //private static Dictionary<string, RunningReductionTask> ExecutingTasks = new Dictionary<string, RunningReductionTask>();

        private Dictionary<int, JobMonitorInfo> JobMonitorDict = new Dictionary<int, JobMonitorInfo>();

        // TODO get this from config
        const string AuditLogCxn = "Server=localhost;Database=MapAuditLog;User Id=postgres;Password=postgres;";

        private string RootPath = string.Empty;

        /// <summary>
        /// constructor, initializes some things (do better)
        /// </summary>
        public ProcessManager()
        {
            if (Configuration.Cfg == null)
            {
                throw new ApplicationException("Application configuration is not initialized");
            }

            AuditLogger.Config = new AuditLoggerConfiguration { AuditLogConnectionString = AuditLogCxn };
        }

        /// <summary>
        /// Initiate processing of configured JobMonitors
        /// </summary>
        /// <param name="ProcessConfig"></param>
        public void Start()
        {
            // Based on configuration, set up objects derived from JobMonitorBase
            // for (int i = 0 ; i < ConfiguredMonitors.Count ; i++)
            for (int i = 0 ; i < 1 ; i++)
            {
                // TODO Need to switch on ConfiguredMonitors type to instantiate the correct type for Monitor property

                JobMonitorDict.Add(i, new JobMonitorInfo
                {
                    Monitor = new MapDbJobMonitor { ConfiguredConnectionStringParamName = "DefaultConnection" },
                    TokenSource = new CancellationTokenSource(),
                    AwaitableTask = null
                });
            }

            // Start the job monitor threads
            foreach (var MonitorKvp in JobMonitorDict)
            {
                JobMonitorInfo MonitorInfo = MonitorKvp.Value;
                MonitorInfo.AwaitableTask = MonitorInfo.Monitor.Start(MonitorInfo.TokenSource.Token);

                Trace.WriteLine($"JobMonitor {MonitorKvp.Key} Start() returned");
            }
        }

        /// <summary>
        /// Entry point intended for the main application to request this object to gracefully stop all processing under its control
        /// </summary>
        /// <param name="WaitMs"></param>
        /// <returns></returns>
        public bool Stop(int WaitMs = 0)
        {
            TimeSpan MaxWaitTime = TimeSpan.FromMinutes(3);

            foreach (var MonitorKvp in JobMonitorDict)
            {
                JobMonitorInfo MonitorInfo = MonitorKvp.Value;
                MonitorInfo.TokenSource.Cancel();

                Trace.WriteLine($"Token {MonitorKvp.Key} cancellation requested");
            }

            // Wait for all the running job monitors to complete
            DateTime Start = DateTime.Now;
            var WaitResult = Task.WaitAll(JobMonitorDict.Select(m => m.Value.AwaitableTask).ToArray(), MaxWaitTime);
            Trace.WriteLine($"WaitAll ran for {DateTime.Now - Start}");

            if (!AnyMonitorThreadRunning)
            {
                JobMonitorDict.Clear();
            }

            return WaitResult;
        }

        /// <summary>
        /// Property that evaluates whether status of all Tasks of managed JobMonitor objects is running
        /// </summary>
        public bool AllMonitorThreadsRunning
        {
            get
            {
                return JobMonitorDict.Values.All(v => v.AwaitableTask.Status == TaskStatus.Running);
            }
        }

        /// <summary>
        /// Property that evaluates whether at least 1 status of all Tasks of managed JobMonitor objects is running
        /// </summary>
        public bool AnyMonitorThreadRunning
        {
            get
            {
                return JobMonitorDict.Values.Any(v => v.AwaitableTask.Status == TaskStatus.Running);
            }
        }

    }
}
