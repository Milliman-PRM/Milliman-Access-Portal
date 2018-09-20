/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A Windows service that invokes the functionality of the ContentReductinLib project
 * DEVELOPER NOTES: Intended to live a parallel with a GUI project, both of which do no 
 * more than is necessary to invoke all features of the library. 
 */

using System;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using ContentPublishingLib;
using MapCommonLib;

namespace ContentPublishingService
{
    public partial class ContentPublishingService : ServiceBase
    {
        ProcessManager Manager = null;
        private TextWriterTraceListener CurrentTraceListener = null;

        public ContentPublishingService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Configuration.LoadConfiguration();

                string ServiceLaunchDelaySecString = Configuration.GetConfigurationValue("ServiceLaunchDelaySec");
                if (!string.IsNullOrWhiteSpace(ServiceLaunchDelaySecString) && int.TryParse(ServiceLaunchDelaySecString, out int ServiceLaunchDelaySec))
                {
                     Thread.Sleep(ServiceLaunchDelaySec * 1000);
                }

                InitiateTraceLogging();
                GlobalFunctions.TraceWriteLine($"Service OnStart() called");

                if (Manager == null || !Manager.AnyMonitorThreadRunning)
                {
                    Manager = new ProcessManager(this.ServiceName);
                    Manager.Start();
                }
            }
            catch (Exception e)
            {
                GlobalFunctions.TraceWriteLine($"Failed to start service, exception:{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Adds a service trace logging file to the collection of Trace listeners
        /// </summary>
        private void InitiateTraceLogging()
        {
            if (CurrentTraceListener == null)
            {
                EventLogEntryType EvtType = EventLogEntryType.Information;
                string EvtMsg = string.Empty;

                string TraceLogDirectory = Configuration.GetConfigurationValue("TraceLogDirectory");
                if (!Directory.Exists(TraceLogDirectory))
                {
                    EvtMsg += $"No configured Tracelog directory, or directory {TraceLogDirectory} does not exist. ";
                    EvtType = EventLogEntryType.Warning;

                    // Get the full path of the assembly in which ContentPublishingService is declared
                    string fullPath = System.Reflection.Assembly.GetAssembly(typeof(ContentPublishingService)).Location;
                    TraceLogDirectory = Path.GetDirectoryName(fullPath);
                }

                string TraceLogFilePath = Path.Combine(TraceLogDirectory, $"QvReportReductionService_Trace_{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.txt");
                EvtMsg += $"Using Trace logging file {Environment.NewLine}    {TraceLogFilePath}";
                EventLog.WriteEntry(EvtMsg, EvtType);

                CurrentTraceListener = new TextWriterTraceListener(Path.Combine(TraceLogDirectory, $"QvReportReductionService_Trace_{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.txt"));
                Trace.Listeners.Add(CurrentTraceListener);
                Trace.AutoFlush = true;
            }
        }

        protected override void OnStop()
        {
            GlobalFunctions.TraceWriteLine($"Service OnStop() called");
            if (Manager != null)
            {
                Manager.Stop();
                Manager = null;
            }
        }

        protected override void OnShutdown()
        {
            GlobalFunctions.TraceWriteLine($"Service OnShutdown() called");
            if (Manager != null)
            {
                Manager.Stop();
            }
            base.OnShutdown();
        }

        #region Unimplemented service callbacks
        protected override void OnPause()
        {
            GlobalFunctions.TraceWriteLine($"Service OnPause() called");

            base.OnPause();
        }

        protected override void OnContinue()
        {
            GlobalFunctions.TraceWriteLine($"Service OnContinue() called");

            base.OnContinue();
        }

        protected override void OnCustomCommand(int command)
        {
            GlobalFunctions.TraceWriteLine($"Service OnCommand() called with command= {command}");
            base.OnCustomCommand(command);

            switch (command)   // must be between 128 and 255
            {
                case 128:
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
