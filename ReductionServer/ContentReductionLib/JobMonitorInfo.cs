using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace ContentReductionLib
{
    internal class JobMonitorInfo
    {
        internal JobMonitorBase Monitor;
        internal CancellationTokenSource TokenSource;
        internal Task AwaitableTask;
    }
}
