/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Base class defining 
 * DEVELOPER NOTES: Does not work when implemented as a struct
 */

using System.Threading;
using System.Threading.Tasks;

namespace ContentReductionLib
{
    internal abstract class JobMonitorBase
    {
        internal abstract Task Start(CancellationToken Token);
        internal abstract void JobMonitorThreadMain(CancellationToken Token);
    }
}
