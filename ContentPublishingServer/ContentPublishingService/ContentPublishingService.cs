/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A Windows service that invokes the functionality of the ContentReductinLib project
 * DEVELOPER NOTES: Intended to live a parallel with a GUI project, both of which do no 
 * more than is necessary to invoke all features of the library. 
 */

using Serilog;
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using ContentPublishingLib;
using MapCommonLib;
using Microsoft.Extensions.Configuration;

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

                int ServiceLaunchDelaySec = Configuration.ApplicationConfiguration.GetValue("ServiceLaunchDelaySec", 0);
                Thread.Sleep(ServiceLaunchDelaySec * 1000);

                Assembly processAssembly = Assembly.GetAssembly(typeof(ContentPublishingService));
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
                string introMsg = $"Process launched:{Environment.NewLine}" +
                                  $"\tProduct Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                                  $"\tassembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                                  $"\tassembly location <{processAssembly.Location}>{Environment.NewLine}" +
                                  $"\tASPNETCORE_ENVIRONMENT = <{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}>{Environment.NewLine}";

                Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(Configuration.ApplicationConfiguration)
                        .CreateLogger();
                Log.Information(introMsg);

                Log.Information($"Service OnStart() called");

                if (Manager == null || !Manager.AnyMonitorThreadRunning)
                {
                    Manager = new ProcessManager(this.ServiceName);
                    Manager.Start();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to start service");
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

                string TraceLogDirectory = Configuration.ApplicationConfiguration.GetValue("TraceLogDirectory", Path.GetDirectoryName(Assembly.GetAssembly(typeof(ContentPublishingService)).Location));

                string TraceLogFilePath = Path.Combine(TraceLogDirectory, $"QvReportReductionService_Trace_{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.txt");
                EvtMsg += $"Using Trace logging file {Environment.NewLine}    {TraceLogFilePath}";
                EventLog.WriteEntry(EvtMsg, EvtType);

                CurrentTraceListener = new TextWriterTraceListener(TraceLogFilePath);
                Trace.Listeners.Add(CurrentTraceListener);
                Trace.AutoFlush = true;
            }
        }

        protected override void OnStop()
        {
            Log.Information($"Service OnStop() called");
            if (Manager != null)
            {
                Manager.Stop();
                Manager = null;
            }
        }

        protected override void OnShutdown()
        {
            Log.Information($"Service OnShutdown() called");
            if (Manager != null)
            {
                Manager.Stop();
            }
            base.OnShutdown();
        }

        #region Unimplemented service callbacks
        protected override void OnPause()
        {
            Log.Information($"Service OnPause() called");

            base.OnPause();
        }

        protected override void OnContinue()
        {
            Log.Information($"Service OnContinue() called");

            base.OnContinue();
        }

        protected override void OnCustomCommand(int command)
        {
            Log.Information($"Service OnCommand() called with command= {command}");
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
