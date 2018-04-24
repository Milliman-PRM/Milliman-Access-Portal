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
using ContentReductionLib.ReductionRunners;
using TestResourcesLib;

namespace ContentReductionServiceTests
{
    public class QvReductionRunnerTests : ContentReductionServiceTestBase
    {
        [Fact]
        public void TestMethod1()
        {
            // TODO initialize data? 

            #region Arrange
            QvReductionRunner TestRunner = new QvReductionRunner
            {
                AuditLog = MockAuditLogger.New().Object,
                JobDetail = (ReductionJobDetail)DbTask,  // TODO get the dbtask from the mock context
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 3, 0));
            #endregion

            #region Assert
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            #endregion
        }

    }
}
