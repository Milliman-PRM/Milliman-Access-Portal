/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;

namespace ContentReductionLib
{
    internal class MapDbJobMonitor : JobMonitorBase
    {
        internal MapDbJobMonitor()
        {

        }

        internal override Task Start(CancellationToken Token)
        {
            return Task.Run(() => Threadmain() /* TODO: , cancellationToken */ );
        }

        internal override void Threadmain()
        {
            for (int i=0; i < 10; i++)
            {
                int count = GetItems(4);
                Trace.WriteLine($"GetItems completed {i} times with {count} responses");
            }
            Trace.WriteLine("Threadmain completed");
        }

        internal override int GetItems(int MaxCount)
        {
            using (var Db=MapDbContextAccessor.New)
            {
                List<ContentReductionTask> TopItems = Db.ContentReductionTask.Where(t => t.CreateDateTime - DateTimeOffset.UtcNow < TimeSpan.FromSeconds(30))
                                                                             .OrderBy(t => t.CreateDateTime)
                                                                             .Take(MaxCount)
                                                                             .ToList();

                return TopItems.Count;
            }
        }
    }
}
