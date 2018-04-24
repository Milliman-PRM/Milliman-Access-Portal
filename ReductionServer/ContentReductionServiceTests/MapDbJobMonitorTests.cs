/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentReductionLib;
using TestResourcesLib;
using Moq;

namespace ContentReductionServiceTests
{
    public class MapDbJobMonitorTests : ContentReductionServiceTestBase
    {
        [Fact]
        public void CorrectStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbJobMonitor JobMonitor = new MapDbJobMonitor
            {
                UseMockForTesting = true,
                MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = JobMonitor.Start(CancelTokenSource.Token);
            Thread.Sleep(new TimeSpan(0, 0, 5));
            #endregion

            #region Assert
            Assert.Equal<TaskStatus>(TaskStatus.Running, MonitorTask.Status);
            #endregion

            #region Act again
            DateTime CancelTime = DateTime.UtcNow;
            CancelTokenSource.Cancel();
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 0, 40));
            #endregion

            #region Assert
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.True(DateTime.UtcNow - CancelTime < new TimeSpan(0,0,30), "MapDbJobMonitor took too long to be canceled while idle");
            #endregion
        }

    }
}
