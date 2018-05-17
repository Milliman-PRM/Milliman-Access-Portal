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
    public class MapDbReductionJobMonitorTests : ContentReductionServiceTestBase
    {
        [Fact]
        public async Task CorrectTaskStatusAfterCancelWhileIdle()
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
            Assert.Equal<TaskStatus>(TaskStatus.Running, MonitorTask.Status);
            #endregion

            #region Act again
            DateTime CancelStartTime = DateTime.UtcNow;
            CancelTokenSource.Cancel();
            try
            {
                await MonitorTask;  // await rethrows anything that is thrown from the task
            }
            catch (OperationCanceledException)  // This is thrown when a task is cancelled
            { }
            DateTime CancelEndTime = DateTime.UtcNow;
            #endregion

            #region Assert again
            Assert.Equal<TaskStatus>(TaskStatus.Canceled, MonitorTask.Status);
            Assert.True(MonitorTask.IsCanceled);
            Assert.True(CancelEndTime - CancelStartTime < new TimeSpan(0,0,30), "MapDbReductionJobMonitor took too long to be canceled while idle");
            #endregion
        }

    }
}
