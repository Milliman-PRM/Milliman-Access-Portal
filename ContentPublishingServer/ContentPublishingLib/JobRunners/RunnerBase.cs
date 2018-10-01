/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: A base class defining common interface methods to all reduction runner classes
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AuditLogLib.Services;

namespace ContentPublishingLib.JobRunners
{
    public abstract class RunnerBase
    {
        #region Member properties
        protected CancellationToken _CancellationToken { get; set; }

        protected IAuditLogger AuditLog = null;
        #endregion

        protected void AssertTesting()
        {
            StackTrace CallStack = new StackTrace();
            bool IsTest = CallStack.GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "ContentPublishingServiceTests") 
                       || System.Environment.CommandLine.Contains("testhost.dll");
            if (!IsTest)
            {
                throw new ApplicationException($"Assert testing failed.  Stack trace:{Environment.NewLine}{CallStack.ToString()}");
            }
        }

        public void SetTestAuditLogger(IAuditLogger LoggerArg)
        {
            AssertTesting();
            AuditLog = LoggerArg;
        }

    }
}
