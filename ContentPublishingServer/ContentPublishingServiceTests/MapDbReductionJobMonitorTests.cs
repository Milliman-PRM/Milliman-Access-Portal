/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContentPublishingLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentPublishingLib.JobMonitors;
using TestResourcesLib;
using Moq;

namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class MapDbReductionJobMonitorTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;
        TestInitialization TestResources;

        public MapDbReductionJobMonitorTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
            TestResources = new TestInitialization(_dbLifeTimeFixture.ConnectionString);
            Configuration.ApplicationConfiguration = (ConfigurationRoot)_dbLifeTimeFixture.Configuration;
        }

        [Fact]
        public async Task CorrectTaskStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbReductionJobMonitor JobMonitor = new MapDbReductionJobMonitor(TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = new SemaphoreSlim(1, 1),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = JobMonitor.StartAsync(CancelTokenSource.Token);
            Thread.Sleep(new TimeSpan(0, 0, 5));
            #endregion

            #region Assert
            Assert.Contains(MonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation });
            #endregion

            #region Act again
            DateTime CancelStartTime = DateTime.UtcNow;
            CancelTokenSource.Cancel();
            try
            {
                await MonitorTask;  // await rethrows anything that is thrown from the task
            }
            catch (OperationCanceledException)  // This is thrown when a task is cancelled
            {}
            DateTime CancelEndTime = DateTime.UtcNow;
            Thread.Sleep(2000);
            #endregion

            #region Assert again
            Assert.Equal<TaskStatus>(TaskStatus.Canceled, MonitorTask.Status);
            Assert.True(MonitorTask.IsCanceled);
            Assert.True(CancelEndTime - CancelStartTime < new TimeSpan(0,0,30), "MapDbReductionJobMonitor took too long to be canceled while idle");
            #endregion
        }

    }
}
