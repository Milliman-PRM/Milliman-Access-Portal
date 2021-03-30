using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace PowerBiMigration
{
    public class OperationScope : IDisposable
    {
        private static Mutex _Mutex = new Mutex();
        private string _operationName = string.Empty;
        private Form _parentForm = null;
        public Stopwatch operationTimer { get; private set; }

        public OperationScope(Form parentForm, string operationName = null)
        {
            _Mutex.WaitOne();

            _operationName = operationName;
            _parentForm = parentForm;

            if (!string.IsNullOrWhiteSpace(operationName))
            {
                Log.Information("StartOperation: " + _operationName);
            }

            _parentForm.UseWaitCursor = true;
            operationTimer = Stopwatch.StartNew();
        }

        public void Dispose() 
        {
            if (!string.IsNullOrWhiteSpace(_operationName))
            {
                Log.Information("EndOperation: " + _operationName);
            }

            _parentForm.UseWaitCursor = false;

            _Mutex.ReleaseMutex();
        }
    }
}
