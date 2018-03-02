/**//*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: POCO to contain various relevant objects about one runnable JobMonitor
 */

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
