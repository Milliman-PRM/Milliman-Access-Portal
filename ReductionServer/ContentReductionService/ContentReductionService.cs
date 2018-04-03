using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ContentReductionLib;

namespace ContentReductionService
{
    public partial class ContentReductionService : ServiceBase
    {
        ProcessManager Manager = null;

        public ContentReductionService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Configuration.GetConfiguration();

            Manager = new ProcessManager();
            Manager.Start();
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
            if (Manager != null)
            {
                Manager.Stop();
            }
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
