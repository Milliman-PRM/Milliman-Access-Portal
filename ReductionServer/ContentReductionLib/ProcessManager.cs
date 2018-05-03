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
using System.ServiceProcess;
using AuditLogLib;
using MapCommonLib;

namespace ContentReductionLib
{
    public class ProcessManager
    {
        // This collection is keyed on the config file name
        //private static Dictionary<string, RunningReductionTask> ExecutingTasks = new Dictionary<string, RunningReductionTask>();

        private Dictionary<int, JobMonitorInfo> JobMonitorDict = new Dictionary<int, JobMonitorInfo>();
        private Timer JobMonitorHealthCheckTimer;
        static TimeSpan HealthCheckInterval = new TimeSpan(0, 0, 10);
        private string ServiceName = string.Empty;

        /// <summary>
        /// constructor, initializes some things (do better)
        /// </summary>
        public ProcessManager(string ServiceNameArg = "")
        {
            if (Configuration.ApplicationConfiguration == null)
            {
                throw new ApplicationException("Application configuration is not initialized");
            }

            AuditLogger.Config = new AuditLoggerConfiguration { AuditLogConnectionString = Configuration.GetConnectionString("AuditLogConnectionString") };

            ServiceName = ServiceNameArg;
        }

        /// <summary>
        /// Checks each JobMonitor for whether it continues to run, maintaining the dictionary that tracks those JobMonitors
        /// </summary>
        /// <param name="state"></param>
        public void JobMonitorHealthCheck(object state)
        {
            for (int DownCounter = JobMonitorDict.Count-1; DownCounter >= 0; DownCounter--)
            {
                JobMonitorInfo MonitorInfo = JobMonitorDict[DownCounter];

                if (MonitorInfo.AwaitableTask.IsCompleted)
                {
                    JobMonitorDict.Remove(DownCounter);
                    Trace.WriteLine($"From ProcessManager, JobMonitor of type {MonitorInfo.Monitor.GetType().Name} ended with task status {MonitorInfo.AwaitableTask.Status.ToString()}.  There are {JobMonitorDict.Count} JobMonitor instances still running");
                    if (MonitorInfo.AwaitableTask.Status == TaskStatus.Faulted)
                    {
                        Trace.WriteLine(GlobalFunctions.LoggableExceptionString(MonitorInfo.AwaitableTask.Exception, "Exception was", true, true));
                        Thread.Sleep(1000);
                    }
                    if (MonitorInfo.AwaitableTask.Status != TaskStatus.Canceled)
                    {
                        // this is intended to crash the service rather than continue running with no activity
                        throw new ApplicationException("JobMonitor ended", MonitorInfo.AwaitableTask.Exception);
                    }
                }
            }

            if (JobMonitorDict.Count == 0)
            {
                // We are probably only here when the JobMonitor object(s) is cancelled, e.g. during a stop or shutdown
                JobMonitorHealthCheckTimer.Dispose();

                if (!string.IsNullOrWhiteSpace(ServiceName))
                {
                    ServiceController sc = new ServiceController(ServiceName);
                    sc.Stop();
                }
            }
        }

        /// <summary>
        /// Initiate processing of configured JobMonitors
        /// </summary>
        /// <param name="ProcessConfig"></param>
        public void Start()
        {
            // TODO Based on configuration, set up objects derived from JobMonitorBase
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

                Trace.WriteLine($"JobMonitor of type {MonitorInfo.Monitor.GetType().Name} started");
            }

            // Initiate periodic checking of the Task status of each JobMonitor
            JobMonitorHealthCheckTimer = new Timer(JobMonitorHealthCheck, null, HealthCheckInterval, HealthCheckInterval);
        }

        /// <summary>
        /// Entry point intended for the main application to request this object to gracefully stop all processing under its control
        /// </summary>
        /// <param name="WaitMs">if negative, use configured parameter "StopWaitTimeSeconds" or default to hard coded value</param>
        /// <returns></returns>
        public bool Stop(int WaitSec = -1)
        {
            JobMonitorHealthCheckTimer.Dispose();

            if (WaitSec < 0)
            {
                if (!int.TryParse(Configuration.ApplicationConfiguration["StopWaitTimeSeconds"], out WaitSec))
                {
                    WaitSec = 3 * 60;
                }
            }
            TimeSpan MaxWaitTime = TimeSpan.FromSeconds(WaitSec);

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
