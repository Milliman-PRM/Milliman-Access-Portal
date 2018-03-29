/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A base class defining common interface methods to all reduction runner classes
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace ContentReductionLib.ReductionRunners
{
    internal abstract class ReductionRunnerBase
    {
        internal abstract Task<ReductionJobResult> ExecuteReduction(CancellationToken cancellationToken);

        internal abstract void ValidateThisInstance();
    }
}
