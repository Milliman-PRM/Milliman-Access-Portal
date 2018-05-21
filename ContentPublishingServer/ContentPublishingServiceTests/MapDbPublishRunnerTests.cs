/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using ContentPublishingLib.JobMonitors;
using ContentPublishingLib.JobRunners;
using TestResourcesLib;
using Moq;
using Xunit;

namespace ContentPublishingServiceTests
{
    public class MapDbPublishRunnerTests : ContentPublishingServiceTestBase
    {
        [Fact]
        public async Task DoesNotReduce_MasterSelectionGroupExists()
        {
            #region Arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the task to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 1);
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                JobDetail = PublishJobDetail.New(DbRequest, MockContext.Object),
                MockContext = MockContext,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);           

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Equal(1, MockContext.Object.SelectionGroup.Count(g => g.RootContentItemId == 1));  // check before
            #endregion

            #region Act
            Task<PublishJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            PublishJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
            Assert.Equal(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.RelatedFiles);
            Assert.Equal(2, TaskResult.RelatedFiles.Count);
            Assert.Equal(1, MockContext.Object.SelectionGroup.Count(g => g.RootContentItemId == 1));  // check after
            Assert.Equal("MasterContent", TaskResult.RelatedFiles[0].FilePurpose);
            Assert.Equal("UserGuide", TaskResult.RelatedFiles[1].FilePurpose);
            #endregion
        }

        [Fact]
        public async Task DoesNotReduce_NoSelectionGroupExists()
        {
            #region Arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the task to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 2);
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                JobDetail = PublishJobDetail.New(DbRequest, MockContext.Object),
                MockContext = MockContext,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Empty(MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId));  // check before
            #endregion

            #region Act
            Task<PublishJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            PublishJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            List<SelectionGroup> AllSelGroups = MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId).ToList();
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
            Assert.Equal(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.RelatedFiles);
            Assert.Equal(2, TaskResult.RelatedFiles.Count);
            Assert.Single(AllSelGroups);
            Assert.True(AllSelGroups.Single().IsMaster);
            Assert.Equal("MasterContent", TaskResult.RelatedFiles[0].FilePurpose);
            Assert.Equal("UserGuide", TaskResult.RelatedFiles[1].FilePurpose);
            #endregion
        }

        [Fact]
        public async Task DoesReduce_NoSelectionGroupExists()
        {
            #region Arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the task to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 3);
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                JobDetail = PublishJobDetail.New(DbRequest, MockContext.Object),
                MockContext = MockContext,
            };
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            MapDbReductionJobMonitor ReductionMonitor = new MapDbReductionJobMonitor
            {
                MockContext = MockContext,
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Empty(MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId));  // check before

            Task TaskMonitorTask = ReductionMonitor.Start(CancelTokenSource.Token);
            #endregion

            #region Act
            Task<PublishJobDetail> RequestRunnerTask = TestRunner.Execute(CancelTokenSource.Token);
            PublishJobDetail JobDetail = await RequestRunnerTask;

            CancelTokenSource.Cancel();  // End the MapDbReductionJobMonitor

            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            List<SelectionGroup> AllSelGroups = MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId).ToList();
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
            Assert.Equal(TaskStatus.RanToCompletion, RequestRunnerTask.Status);
            Assert.False(RequestRunnerTask.IsCanceled);
            Assert.False(RequestRunnerTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, RequestRunnerTask.Result.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.RelatedFiles);
            Assert.Equal(2, TaskResult.RelatedFiles.Count);
            Assert.Single(AllSelGroups);
            Assert.True(AllSelGroups.Single().IsMaster);
            Assert.Equal("MasterContent", TaskResult.RelatedFiles[0].FilePurpose);
            Assert.Equal("UserGuide", TaskResult.RelatedFiles[1].FilePurpose);
            #endregion
        }

        [Fact]
        public async Task DoesReduce_TwoSelectionGroupsExist()
        {
            #region Arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the task to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 4);
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                JobDetail = PublishJobDetail.New(DbRequest, MockContext.Object),
                MockContext = MockContext,
            };
            int InitialTaskCount = MockContext.Object.ContentReductionTask.Count(t => t.ContentPublicationRequestId == DbRequest.Id);
            TestRunner.SetTestAuditLogger(MockAuditLogger.New().Object);

            MapDbReductionJobMonitor ReductionMonitor = new MapDbReductionJobMonitor
            {
                MockContext = MockContext,
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();

            // check before
            Assert.Equal(2, MockContext.Object.SelectionGroup.Count(g => g.RootContentItemId == DbRequest.RootContentItemId));
            Assert.Empty(MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId && g.IsMaster));

            Task TaskMonitorTask = ReductionMonitor.Start(CancelTokenSource.Token);
            #endregion

            #region Act
            Task<PublishJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            PublishJobDetail JobDetail = await MonitorTask;

            CancelTokenSource.Cancel();  // End the MapDbReductionJobMonitor

            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            List<SelectionGroup> AllSelGroups = MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId).ToList();
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
            Assert.Equal(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.RelatedFiles);
            Assert.Equal(2, TaskResult.RelatedFiles.Count);
            Assert.Equal(2, MockContext.Object.SelectionGroup.Count(g => g.RootContentItemId == DbRequest.RootContentItemId));
            Assert.Equal(InitialTaskCount + 2, MockContext.Object.ContentReductionTask.Count(t => t.ContentPublicationRequestId == DbRequest.Id 
                                                                                               && t.ReductionStatus == ReductionStatusEnum.Reduced));
            Assert.Empty(MockContext.Object.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId && g.IsMaster));
            Assert.Equal("MasterContent", TaskResult.RelatedFiles[0].FilePurpose);
            Assert.Equal("UserGuide", TaskResult.RelatedFiles[1].FilePurpose);
            #endregion
        }
    }
}
