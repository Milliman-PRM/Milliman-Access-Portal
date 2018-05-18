/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using ContentPublishingLib.JobRunners;
using TestResourcesLib;
using Moq;
using Xunit;

namespace ContentPublishingServiceTests
{
    public class MapDbPublishRunnerTests : ContentPublishingServiceTestBase
    {
        [Fact]
        public async Task HappyPathDoesNotReduce()
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
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, MonitorTask.Result.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.RelatedFiles);
            Assert.Equal(2, TaskResult.RelatedFiles.Count);
            Assert.Equal("MasterContent", TaskResult.RelatedFiles[0].FilePurpose);
            Assert.Equal("UserGuide", TaskResult.RelatedFiles[1].FilePurpose);
            #endregion
        }
    }
}
