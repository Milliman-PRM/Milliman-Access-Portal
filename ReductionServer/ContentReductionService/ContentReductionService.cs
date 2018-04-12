/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A Windows service that invokes the functionality of the ContentReductinLib project
 * DEVELOPER NOTES: Intended to live a parallel with a GUI project, both of which do no 
 * more than is necessary to invoke all features of the library. 
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContentReductionLib;

namespace ContentReductionService
{
    public partial class ContentReductionService : ServiceBase
    {
        ProcessManager Manager = null;
        private TextWriterTraceListener CurrentTraceListener = null;

        public ContentReductionService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread.Sleep(12000);
            try
            {
                Trace.WriteLine($"Service OnStart() called");
                Configuration.LoadConfiguration();

                InitiateTraceLogging();

                if (Manager == null || !Manager.AnyMonitorThreadRunning)
                {
                    Manager = new ProcessManager();
                    Manager.Start();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to launch service, exception:{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                throw;
            }
        }

        private void InitiateTraceLogging()
        {
            if (CurrentTraceListener == null)
            {
                string TraceLogDirectory = Configuration.ApplicationConfiguration["TraceLogDirectory"];
                if (!Directory.Exists(TraceLogDirectory))
                {
                    EventLog.WriteEntry($"No configured Tracelog directory, or directory {TraceLogDirectory} does not exist", EventLogEntryType.Warning);
                    TraceLogDirectory = @"C:\temp\";
                }

                string TraceLogFilePath = Path.Combine(TraceLogDirectory, $"QvReportReductionService_Trace_{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.txt");
                EventLog.WriteEntry($"Using Trace logging file {TraceLogFilePath}", EventLogEntryType.Warning);

                CurrentTraceListener = new TextWriterTraceListener(Path.Combine(TraceLogDirectory, $"QvReportReductionService_Trace_{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.txt"));
                Trace.Listeners.Add(CurrentTraceListener);
                Trace.AutoFlush = true;
            }
        }

        protected override void OnStop()
        {
            Trace.WriteLine($"Service OnStop() called");
            if (Manager != null)
            {
                Manager.Stop();
                Manager = null;
            }
        }

        protected override void OnShutdown()
        {
            Trace.WriteLine($"Service OnShutdown() called");
            if (Manager != null)
            {
                Manager.Stop();
            }
            base.OnShutdown();
        }

        #region Unimplemented service callbacks
        protected override void OnPause()
        {
            Trace.WriteLine($"Service OnPause() called");

            base.OnPause();
        }

        protected override void OnContinue()
        {
            Trace.WriteLine($"Service OnContinue() called");

            base.OnContinue();
        }

        protected override void OnCustomCommand(int command)
        {
            Trace.WriteLine($"Service OnCommand() called with command= {command}");
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
