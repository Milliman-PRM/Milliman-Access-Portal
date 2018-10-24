/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Base class defining common elements of any concreate JobMonitor type
 * DEVELOPER NOTES: 
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Moq;
using MapDbContextLib.Context;

namespace ContentPublishingLib.JobMonitors
{
    public abstract class JobMonitorBase
    {
        public abstract Task Start(CancellationToken Token);
        public abstract void JobMonitorThreadMain(CancellationToken Token);

        internal abstract string MaxConcurrentRunnersConfigKey { get; }

        /// <summary>
        /// Can be provided by test code to initializate data in a mocked ApplicationDbContext
        /// </summary>
        private Mock<ApplicationDbContext> _MockContext = null;
        public Mock<ApplicationDbContext> MockContext
        {
            protected get
            {
                return _MockContext;
            }
            set
            {
                AssertTesting();
                _MockContext = value;
            }
        }

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
    }
}
