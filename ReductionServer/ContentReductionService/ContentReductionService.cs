/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A Windows service that invokes the functionality of the ContentReductinLib project
 * DEVELOPER NOTES: Intended to live a parallel with a GUI project, both of which do no 
 * more than is necessary to invoke all features of the library. 
 */

using System;
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
            DateTime StartDateTime = DateTime.Now;
            CurrentTraceListener = new TextWriterTraceListener(@"C:\temp\QvReportReductionService_Trace_" + StartDateTime.ToString("yyyyMMdd-HHmmss") + ".txt");
            Trace.Listeners.Add(CurrentTraceListener);
            Trace.AutoFlush = true;

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Trace.WriteLine($"Service OnStart() called");
            Configuration.LoadConfiguration();

            Manager = new ProcessManager();
            Manager.Start();
        }

        protected override void OnStop()
        {
            Trace.WriteLine($"Service OnStop() called");
            if (Manager == null)
            {
                Manager.Stop();
                Manager = null;
            }
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

        protected override void OnShutdown()
        {
            Trace.WriteLine($"Service OnShutdown() called");
            if (Manager != null)
            {
                Manager.Stop();
            }
            base.OnShutdown();
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
