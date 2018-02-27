using System;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ContentReductionLibOld;
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

            textBox1.Text = ConfigurationManager.AppSettings["RootPath"];
            numericUpDownThreads.Value = int.Parse(ConfigurationManager.AppSettings["MaxConcurrentTasks"]);

            timer1.Interval = 1000;
            timer1.Start();
        }

        private void ButtonInitiateLoop_Click(object sender, EventArgs e)
        {
            Manager = new ProcessManager();

            ProcessManagerConfiguration ProcessConfig = new ProcessManagerConfiguration
            {
                RootPath = textBox1.Text,
                MaxConcurrentTasks = (int)numericUpDownThreads.Value,
            };

            Trace.WriteLine(DateTime.Now + " - " + ProcessConfig.ToString());

            Manager.Start(ProcessConfig);
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
                if (Manager.ThreadAlive)
                {
                    Manager.Stop(WaitMs);
                }

                label1.Text = Manager.ThreadAlive.ToString();

                if (!Manager.ThreadAlive)
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
            label1.Text = (Manager == null) ? "Null Manager" : Manager.ThreadAlive.ToString();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK &&
                Directory.Exists(folderBrowserDialog1.SelectedPath))
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
