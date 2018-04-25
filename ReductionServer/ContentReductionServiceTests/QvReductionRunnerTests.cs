/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentReductionLib.ReductionRunners;
using TestResourcesLib;

namespace ContentReductionServiceTests
{
    public class QvReductionRunnerTests : ContentReductionServiceTestBase
    {
        [Fact]
        public void SuccessfulHierarchyOnly()
        {
            var MockContext = MockMapDbContext.New(InitializeTests.InitializeWithQueuedStatus).Object;
            var DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
            DbTask.TaskAction = MapDbContextLib.Context.TaskActionEnum.HierarchyOnly;

            #region Arrange
            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 3, 0));
            var TaskReturn = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskReturn);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskReturn);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(JobStatusEnum.Success, TaskReturn.Status);

            Assert.NotNull(TaskReturn.MasterContentHierarchy);
            Assert.Equal(3, TaskReturn.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskReturn.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives warning
            Assert.Equal(7, TaskReturn.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskReturn.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.Null(TaskReturn.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskReturn.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskReturn.ReducedContentFileChecksum));
            #endregion
        }

        [Fact]
        public void SuccessfulHierarchyAndReduction()
        {
            var MockContext = MockMapDbContext.New(InitializeTests.InitializeWithQueuedStatus).Object;
            var DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

            #region Arrange
            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 3, 0));
            var TaskResult = MonitorTask.Result.Result;
            var TaskRequest = MonitorTask.Result.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(JobStatusEnum.Success, TaskResult.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.NotNull(TaskResult.ReducedContentHierarchy);
            Assert.Equal(3, TaskResult.ReducedContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.ReducedContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives warning
            Assert.Equal(2, TaskResult.ReducedContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(15, TaskResult.ReducedContentHierarchy.Fields[2].FieldValues.Count);

            Assert.False(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.Equal(TaskRequest.MasterFilePath.Replace(".qvw", ".reduced.qvw"), TaskResult.ReducedContentFilePath);

            Assert.False(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Equal(40, TaskResult.ReducedContentFileChecksum.Length);  
            // Can't know an expected checksum value because it's different every time. 
            #endregion
        }

    }
}
