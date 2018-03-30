/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: A base class defining common interface methods to all reduction runner classes
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Threading;
using System.Threading.Tasks;

namespace ContentReductionLib.ReductionRunners
{
    internal abstract class ReductionRunnerBase
    {
        internal abstract Task<ReductionJobResult> ExecuteReduction(CancellationToken cancellationToken);

        internal abstract void ValidateThisInstance();
    }
}
