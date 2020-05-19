/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Base class defining common elements of any concreate JobMonitor type
 * DEVELOPER NOTES: 
 */

using AuditLogLib.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MapDbContextLib.Context;

namespace ContentPublishingLib.JobMonitors
{
    public abstract class JobMonitorBase
    {
        protected JobMonitorBase(IAuditLogger testAuditLogger)
        {
            JobMonitorInstanceCounter++;

            if (testAuditLogger != null)
            {
                TestAuditLogger = testAuditLogger;
            }
        }

        ~JobMonitorBase()
        {
            JobMonitorInstanceCounter--;
        }

        protected IAuditLogger TestAuditLogger { get; set; } = null;

        protected static int JobMonitorInstanceCounter = 0;

        public abstract Task StartAsync(CancellationToken Token);
        public abstract Task JobMonitorThreadMainAsync(CancellationToken Token);

        internal abstract string MaxConcurrentRunnersConfigKey { get; }

        protected TimeSpan StopWaitTimeSeconds
        {
            get
            {
                int WaitSec;
                try
                {
                    if (!int.TryParse(Configuration.ApplicationConfiguration["StopWaitTimeSeconds"], out WaitSec))
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    // Increases the total time based on concurrent tasks, but less than linearly
                    WaitSec = 3 * 60 * (int)Math.Ceiling(Math.Sqrt(MaxConcurrentRunners));
                }
                return TimeSpan.FromSeconds(WaitSec);
            }
        }

        private int? _maxConcurrentRunners = null;
        public int MaxConcurrentRunners
        {
            get
            {
                if (!_maxConcurrentRunners.HasValue)
                {
                    _maxConcurrentRunners = int.Parse(
                        Configuration.ApplicationConfiguration[MaxConcurrentRunnersConfigKey] ?? "1");
                }
                return _maxConcurrentRunners.Value;
            }
            set { _maxConcurrentRunners = value; }
        }

        protected void AssertTesting()
        {
            StackTrace CallStack = new StackTrace();
            bool IsTest = CallStack.GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "ContentPublishingServiceTests");
            if (!IsTest)
            {
                throw new ApplicationException($"Assert testing failed.  Stack trace:{Environment.NewLine}{CallStack.ToString()}");
            }
        }

        static protected SemaphoreSlim _CleanupOnStartSemaphore = new SemaphoreSlim(1, 1);
        public virtual Task CleanupOnStartAsync() { throw new NotImplementedException(); }
    }
}
