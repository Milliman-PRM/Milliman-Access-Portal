using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace ContentReductionLib
{
    internal abstract class JobMonitorBase
    {
        internal abstract Task Start(CancellationToken Token);
        internal abstract void Threadmain();
        internal abstract int GetItems(int MaxCount);
    }
}
