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

namespace ContentPublishingLib
{
    public abstract class JobMonitorBase
    {
        public abstract Task Start(CancellationToken Token);
        public abstract void JobMonitorThreadMain(CancellationToken Token);

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
