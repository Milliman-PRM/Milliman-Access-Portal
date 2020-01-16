/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Prm.SerilogCustomization;
using Serilog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using ContentPublishingLib;

namespace QvReportReductionGui
{
    public partial class Form1 : Form
    {
        //private TextWriterTraceListener CurrentTraceListener = null;
        ProcessManager Manager = null;

        public Form1()
        {
            Assembly processAssembly = Assembly.GetEntryAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
            string introMsg = $"Process launched:{Environment.NewLine}" +
                              $"\tProduct Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                              $"\tAssembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                              $"\tAssembly location <{processAssembly.Location}>{Environment.NewLine}" +
                              $"\tASPNETCORE_ENVIRONMENT = <{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}>{Environment.NewLine}";

            Configuration.LoadConfiguration();

            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration.ApplicationConfiguration)
                    .Enrich.With<UtcTimestampEnricher>()
                    .CreateLogger();
            Log.Information(introMsg);

            InitializeComponent();

            timer1.Interval = 1000;
            timer1.Start();
        }

        private void ButtonInitiateLoop_Click(object sender, EventArgs e)
        {
            if (Manager == null || !Manager.AnyMonitorThreadRunning)
            {
                Manager = new ProcessManager();
                Manager.Start();
            }
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopProcessing();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopProcessing();
        }

        private void StopProcessing(int WaitSec = -1)
        {
            if (Manager != null)
            {
                Manager.Stop(WaitSec);

                label1.Text = Manager.AllMonitorThreadsRunning.ToString();

                if (!Manager.AnyMonitorThreadRunning)
                {
                    Manager = null;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = (Manager == null) 
                            ? "Null Manager" 
                            : Manager.AllMonitorThreadsRunning.ToString();
        }

    }
}
