/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
//using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ContentReductionLib;

namespace QvReportReductionGui
{
    public partial class Form1 : Form
    {
        private TextWriterTraceListener CurrentTraceListener = null;
        ProcessManager Manager = null;

        public Form1()
        {
            DateTime StartDateTime = DateTime.Now;
            CurrentTraceListener = new TextWriterTraceListener("QvReportReductionService_Trace_" + StartDateTime.ToString("yyyyMMdd-HHmmss") + ".txt");
            Trace.Listeners.Add(CurrentTraceListener);
            Trace.AutoFlush = true;

            InitializeComponent();

            timer1.Interval = 1000;
            timer1.Start();
        }

        private void ButtonInitiateLoop_Click(object sender, EventArgs e)
        {
            Configuration.LoadConfiguration();

            Manager = new ProcessManager();
            Manager.Start();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopProcessing();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopProcessing();
        }

        private void StopProcessing(int WaitMs = 10)
        {
            if (Manager != null)
            {
                Manager.Stop(WaitMs);

                label1.Text = Manager.AllMonitorThreadsRunning.ToString();

                if (!Manager.AnyMonitorThreadRunning)
                {
                    Manager = null;
                }
            }

            if (CurrentTraceListener != null)
            {
                Trace.Listeners.Remove(CurrentTraceListener);
                CurrentTraceListener.Close();
                CurrentTraceListener = null;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = (Manager == null) ? "Null Manager" : Manager.AllMonitorThreadsRunning.ToString();
        }

    }
}
