/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Base class defining 
 * DEVELOPER NOTES: Does not work when implemented as a struct
 */

using System.Threading;
using System.Threading.Tasks;

namespace ContentReductionLib
{
    public abstract class JobMonitorBase
    {
        public abstract Task Start(CancellationToken Token);
        public abstract void JobMonitorThreadMain(CancellationToken Token);
    }
}
