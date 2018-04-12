using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ContentReductionLib;

namespace QvReportReductionService
{
    public partial class QvReportReductionService : ServiceBase
    {
        ProcessManager Manager = null;

        public QvReportReductionService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Manager = new ProcessManager();

            ProcessManagerConfiguration ProcessConfig = new ProcessManagerConfiguration
            {
                RootPath = ConfigurationManager.AppSettings["RootPath"],
                MaxConcurrentTasks = int.Parse(ConfigurationManager.AppSettings["MaxConcurrentTasks"]),
            };

            Manager.Start(ProcessConfig);
        }

        protected override void OnStop()
        {
            Manager.Stop();
            Manager = null;
        }

        #region Unimplemented service callbacks
        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        protected override void OnCustomCommand(int command)
        {
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
