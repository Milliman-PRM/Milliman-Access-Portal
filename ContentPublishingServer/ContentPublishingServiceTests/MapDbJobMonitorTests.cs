/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentPublishingLib.JobMonitors;
using TestResourcesLib;
using Moq;

namespace ContentPublishingServiceTests
{
    public class MapDbJobMonitorTests : ContentReductionServiceTestBase
    {
        [Fact]
        public async Task CorrectStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbReductionJobMonitor JobMonitor = new MapDbReductionJobMonitor
            {
                MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = JobMonitor.Start(CancelTokenSource.Token);
            Thread.Sleep(new TimeSpan(0, 0, 5));
            #endregion

            #region Assert
            System.Console.Out.WriteLine($"First assert, MonitorTask.Status is {MonitorTask.Status}");
            Assert.Equal<TaskStatus>(TaskStatus.Running, MonitorTask.Status);
            #endregion

            #region Act again
            DateTime CancelStartTime = DateTime.UtcNow;
            CancelTokenSource.Cancel();
            try
            {
                await MonitorTask;  // await rethrows anything that is thrown from the task
            }
            catch (AggregateException)  // This is thrown when a task is cancelled
            { }
            DateTime CancelEndTime = DateTime.UtcNow;
            #endregion

            #region Assert again
            System.Console.Out.WriteLine($"Second assert, MonitorTask.Status is {MonitorTask.Status}");
            Assert.Equal<TaskStatus>(TaskStatus.Canceled, MonitorTask.Status);
            System.Console.Out.WriteLine($"Second assert, MonitorTask.IsCanceled is {MonitorTask.IsCanceled}");
            Assert.True(MonitorTask.IsCanceled);
            System.Console.Out.WriteLine($"Second assert, CancelStart is {CancelStartTime}, CancelEnd is {CancelEndTime}, difference is {CancelEndTime-CancelStartTime}");
            Assert.True(CancelEndTime - CancelStartTime < new TimeSpan(0,0,30), "MapDbJobMonitor took too long to be canceled while idle");
            #endregion
        }

    }
}
