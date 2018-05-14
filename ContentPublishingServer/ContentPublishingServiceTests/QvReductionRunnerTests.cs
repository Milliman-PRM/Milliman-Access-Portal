/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Test cases to evaluate QvReductionRunner features. 
 * DEVELOPER NOTES: Depends on an actual Qlikview Publisher to perform content reduction. 
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MapDbContextLib.Context;
using ContentPublishingLib.JobRunners;
using TestResourcesLib;

namespace ContentPublishingServiceTests
{
    public class QvReductionRunnerTests : ContentReductionServiceTestBase
    {
        [Fact]
        public void SuccessfulHierarchyOnly()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
            DbTask.TaskAction = MapDbContextLib.Context.TaskActionEnum.HierarchyOnly;
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskResult = MonitorTask.Result.Result;
            var TaskRequest = MonitorTask.Result.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            if (!string.IsNullOrEmpty(TaskResult.StatusMessage))
            {
                System.Console.WriteLine($"TaskResult.StatusMessage is {TaskResult.StatusMessage}");
            }
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.Null(TaskResult.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            #endregion
        }

        [Fact]
        public void SuccessfulHierarchyAndReduction()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskRequest = MonitorTask.Result.Request;
            var TaskResult = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            if (!string.IsNullOrEmpty(TaskResult.StatusMessage))
            {
                System.Console.WriteLine($"TaskResult.StatusMessage is {TaskResult.StatusMessage}");
            }
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.NotNull(TaskResult.ReducedContentHierarchy);
            Assert.Equal(3, TaskResult.ReducedContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.ReducedContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(2, TaskResult.ReducedContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(15, TaskResult.ReducedContentHierarchy.Fields[2].FieldValues.Count);

            Assert.False(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.Equal(TaskRequest.MasterFilePath.Replace(".qvw", ".reduced.qvw"), TaskResult.ReducedContentFilePath);
            Assert.True(File.Exists(TaskResult.ReducedContentFilePath));

            Assert.False(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Equal(40, TaskResult.ReducedContentFileChecksum.Length);  
            // Can't know an expected checksum value because it's different every time. 
            #endregion
        }

        [Fact]
        public void InvalidSelectionFieldName()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4));
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskRequest = MonitorTask.Result.Request;
            var TaskResult = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Error, MonitorTask.Result.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal to test for 1 == count gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.Null(TaskResult.ReducedContentHierarchy);

            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));

            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Matches("^(The requested reduction field).*(is not found in the reduction hierarchy)$", TaskResult.StatusMessage);
            #endregion
        }

        [Fact]
        public void InvalidSelectionValue()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(2,2,2,2,2,2,2,2,2,2,2));
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskRequest = MonitorTask.Result.Request;
            var TaskResult = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Error, MonitorTask.Result.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);
            Assert.Null(TaskResult.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            //Assert.Matches("^(The requested reduction field).*(is not found in the reduction hierarchy)$", TaskResult.StatusMessage);
            #endregion
        }

        [Fact]
        public void OneValidOneInvalidSelectionValue()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3));
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskRequest = MonitorTask.Result.Request;
            var TaskResult = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            if (!string.IsNullOrEmpty(TaskResult.StatusMessage))
            {
                System.Console.WriteLine($"TaskResult.StatusMessage is {TaskResult.StatusMessage}");
            }
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.NotNull(TaskResult.ReducedContentHierarchy);
            Assert.Equal(3, TaskResult.ReducedContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.ReducedContentHierarchy.Fields[0].FieldValues);
            Assert.Single(TaskResult.ReducedContentHierarchy.Fields[1].FieldValues);
            Assert.Equal(7, TaskResult.ReducedContentHierarchy.Fields[2].FieldValues.Count);

            Assert.Equal(TaskRequest.MasterFilePath.Replace(".qvw", ".reduced.qvw"), TaskResult.ReducedContentFilePath);
            Assert.Equal(40, TaskResult.ReducedContentFileChecksum.Length);
            Assert.True(File.Exists(TaskResult.ReducedContentFilePath));
            #endregion
        }

        [Fact]
        public void MasterFileMissing()
        {
            #region Arrange
            ApplicationDbContext MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus).Object;

            // Modify the task to be tested
            ContentReductionTask DbTask = MockContext.ContentReductionTask.Single(t => t.Id == new Guid(1,1,1,1,1,1,1,1,1,1,1));
            DbTask.ReductionStatus = MapDbContextLib.Context.ReductionStatusEnum.Queued;
            DbTask.MasterFilePath = Path.ChangeExtension(DbTask.MasterFilePath, "xyz");

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            Task.WaitAll(new Task[] { MonitorTask }, new TimeSpan(0, 5, 0));
            var TaskRequest = MonitorTask.Result.Request;
            var TaskResult = MonitorTask.Result.Result;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Error, MonitorTask.Result.Status);
            Assert.Null(TaskResult.MasterContentHierarchy);
            Assert.Null(TaskResult.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Matches("^(Master file).*(does not exist)$", TaskResult.StatusMessage);
            #endregion
        }
    }
}
