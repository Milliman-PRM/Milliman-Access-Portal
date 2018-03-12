/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Intended as the main library API for use by applications.  
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using Milliman.ReductionEngine;
using Milliman.Common;  // TODO remove this
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

        /// <summary>
        /// class used to pass operational parameters to the thread that handles tasks of a single config file
        /// </summary>
        private class ReductionTaskThreadArgs
        {
            internal string ConfigFileName;
            internal ReduceConfig TaskConfig;
        }

        private string RootPath = string.Empty;

        /// <summary>
        /// constructor, initializes some things (do better)
        /// </summary>
        public ProcessManager()
        {
            AuditLogger.Config = new AuditLoggerConfiguration { AuditLogConnectionString = AuditLogCxn };
        }

        /// <summary>
        /// Initiate processing of configured JobMonitors
        /// </summary>
        /// <param name="ProcessConfig"></param>
        public void Start(ProcessManagerConfiguration ProcessConfig)
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

        /*
        /// <summary>
        /// Thread main function that finds runnable config files and initiates processing on each one
        /// </summary>
        /// <param name="Arg">Operating parameters (must be type ProcessManagerConfiguration)</param>
        private void WorkerThreadMain(Object Arg)
        {
            ProcessManagerConfiguration ProcessConfig = Arg as ProcessManagerConfiguration;

            Trace.WriteLine("In " + this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            while (!StopSignal)
            {
                // Clean up any completed tasks
                lock (ExecutingTasks)
                {
                    for (int i = ExecutingTasks.Keys.Count - 1; i >= 0; i--)  // count down from the high index so Remove() doesn't disturb the collection elements
                    {
                        string ConfigFileName = ExecutingTasks.Keys.ElementAt(i);

                        Trace.WriteLine(string.Format("ThreadState {0} for config file {1}", ExecutingTasks[ConfigFileName].Thd.ThreadState.ToString(), ConfigFileName));
                        if (ExecutingTasks[ConfigFileName].Thd.ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            Trace.WriteLine(string.Format("Thread state Stopped, removing from management: {0}", ConfigFileName));
                            ExecutingTasks.Remove(ConfigFileName);
                        }
                    }
                }

                Trace.WriteLine(string.Format("After cleanup, {0} threads managed", ExecutingTasks.Count));

                // Identify new tasks to initiate, up to configured limit
                // TODO Decide whether to implement a recursive search.  This currently supports search only in immediate subfolders (might be best). 
                foreach (string TaskFolderName in Directory.EnumerateDirectories(ProcessConfig.RootPath)
                                                           //.Where(d => ProcessManager.FolderContainsAReductionRequest(d))   // only include valid, ready tasks
                                                           .OrderBy(d => new DirectoryInfo(d).CreationTime))                // order by oldest first (treat the file system like a queue)
                {
                    Trace.WriteLine("Found task(s) in folder " + TaskFolderName);

                    lock (ExecutingTasks)
                    {
                        foreach (string ConfigFileName in Directory.EnumerateFiles(TaskFolderName, "*.config")
                                                                   //.Where(f => ProcessManager.FileIsAvailableToStart(f))   // only include files that are appropriate to start
                                                                   .Take(ProcessConfig.MaxConcurrentTasks - ExecutingTasks.Count))  // Don't exceed the concurrent task count limit
                        {
                            Trace.WriteLine("Initiating processing on config file " + ConfigFileName);

                            // deserialize the config to a class instance
                            ReduceConfig TaskCfg = ReduceConfig.Deserialize(ConfigFileName);

                            // start the thread to process the task
                            Thread Worker = new Thread(InitiateReduction);
                            Worker.Start(new RuductionTaskThreadArgs { ConfigFileName = ConfigFileName, TaskConfig = TaskCfg });

                            // remember the work being done
                            ExecutingTasks.Add(ConfigFileName, new RunningReductionTask { TaskConfig = TaskCfg, Thd = Worker });
                        }

                        if (ProcessConfig.MaxConcurrentTasks == ExecutingTasks.Count)
                        {
                            // don't need to check any more folders right now
                            break;
                        }
                    } // lock (ExecutingTasks)
                }

                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Performs initial validation of all conditions necessary to run a reduction.
        /// This function assumes it is invoked in its own thread, concurrently with the threads for other config files
        /// </summary>
        /// <param name="ConfigFilePath">The path for the config file that will be processed by the thread</param>
        /// <param name="TaskConfig">The <paramref name="ReduceConfig">Config</paramref> object deserialized from disk file</param>
        private void InitiateReduction(object Args)
        {
            string ConfigFilePath = (Args as RuductionTaskThreadArgs).ConfigFileName;
            ReduceConfig TaskConfig = (Args as RuductionTaskThreadArgs).TaskConfig;

            Trace.WriteLine(string.Format("Initializing dedicated thread for processing of config file '{0}'", ConfigFilePath));
            try
            {
                string TaskFolder = Path.GetDirectoryName(ConfigFilePath);
                string MasterQvwFullPath = Path.Combine(TaskFolder, TaskConfig.MasterQVW);

                string FlagFileName = Path.Combine(TaskFolder, string.Format("{0}_running.txt", Path.GetFileNameWithoutExtension(ConfigFilePath)));
                File.Create(FlagFileName).Close();
                Trace.WriteLine(string.Format("Created processing flag file '{0}'", FlagFileName));

                if (!File.Exists(MasterQvwFullPath))
                {
                    Trace.WriteLine(string.Format("Config file is pointing to an unavailable QVW file '{0}'. The process will be terminated...", MasterQvwFullPath));
                    return;
                }
                else
                {
                    Trace.WriteLine(string.Format("Found processable QVW file on '{0}'", MasterQvwFullPath));
                }

                XMLFileSignature Signature = new XMLFileSignature(MasterQvwFullPath);
                bool CanEmit = false;
                if (!string.IsNullOrEmpty(Signature.ErrorMessage))
                {
                    Trace.WriteLine(Signature.ErrorMessage);
                    return;
                }
                else if (!Signature.SignatureDictionary.Keys.Contains("can_emit")
                      || !bool.TryParse(Signature.SignatureDictionary["can_emit"], out CanEmit)
                      || !CanEmit)
                {
                    Trace.WriteLine(string.Format("File '{0}' marked to not be processed. Process will be terminated with success.", TaskConfig.MasterQVW));
                    return;
                }
                else
                {
                    Trace.WriteLine(string.Format("File '{0}' is correctly signed and marked to be processed. Shipping task to ReductionRunner", TaskConfig.MasterQVW));
                    // Get QMS connection parameters
                    QMSSettings QmsSettings = new QMSSettings
                    {
                        //QMSURL = System.Configuration.ConfigurationManager.AppSettings["QMS"],
                        //UserName = System.Configuration.ConfigurationManager.AppSettings["QMSUser"],
                        //Password = System.Configuration.ConfigurationManager.AppSettings["QMSPassword"],
                    };

                    // Create the Runner and start the processing
                    ReductionRunner Runner = new ReductionRunner(QmsSettings);
                    Runner.ConfigFileContent = TaskConfig;
                    Runner.QVWOriginalFullFileName = Path.Combine(Path.GetDirectoryName(ConfigFilePath), TaskConfig.MasterQVW);
                    Runner.Run();
                    Trace.WriteLine("ReductionRunner.Run() process returned for config " + ConfigFilePath);
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error while processing config file '{0}', exception:\r\n{1}\r\n{2}", ConfigFilePath, ex.Message, ex.StackTrace));
                return;
            }
        }
        */
    }
}