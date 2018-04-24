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

namespace ContentReductionLib.ReductionRunners
{
    public abstract class ReductionRunnerBase
    {
        public abstract Task<ReductionJobDetail> Execute(CancellationToken cancellationToken);

        internal abstract void ValidateThisInstance();

        protected void AssertTesting()
        {
            StackTrace CallStack = new StackTrace();
            bool IsTest = CallStack.GetFrames().Any(f => f.GetMethod().DeclaringType.Namespace == "ContentReductionServiceTests");
            if (!IsTest)
            {
                throw new ApplicationException($"Assert testing failed.  Stack trace:{Environment.NewLine}{CallStack.ToString()}");
            }
        }

    }
}
