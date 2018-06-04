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
using ContentPublishingLib.JobMonitors;
using TestResourcesLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Moq;

namespace ContentPublishingServiceTests
{
    public class MapDbPublishJobMonitorTests : ContentPublishingServiceTestBase
    {
        [Fact]
        public async Task CorrectRequestStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbPublishJobMonitor JobMonitor = new MapDbPublishJobMonitor
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
            Assert.True(CancelEndTime - CancelStartTime < new TimeSpan(0, 0, 30), "MapDbPublishJobMonitor took too long to be canceled while idle");
            #endregion
        }

        [Fact]
        public async Task CorrectResultsAfterSuccessfulRun()
        {
            #region arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 1);
            DbRequest.PublishRequest = new PublishRequest
            {
                RootContentItemId = 1,
                RelatedFiles = new UploadedRelatedFile[]
                {
                    new UploadedRelatedFile{FilePurpose="MasterContent", FileUploadId = MockContext.Object.FileUpload.ElementAt(1).Id },
                    new UploadedRelatedFile{FilePurpose="Thumbnail", FileUploadId = MockContext.Object.FileUpload.ElementAt(2).Id },
                }
            };
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishJobMonitor JobMonitor = new MapDbPublishJobMonitor
            {
                MockContext = MockContext,
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = JobMonitor.Start(CancelTokenSource.Token);
            Thread.Sleep(1000);
            Assert.Equal(TaskStatus.Running, MonitorTask.Status);
            Assert.Equal(PublicationStatus.Processing, DbRequest.RequestStatus);

            while (DbRequest.RequestStatus == PublicationStatus.Processing)
            {
                Thread.Sleep(500);
            }
            #endregion

            #region Assert again
            Assert.Equal(TaskStatus.Running, MonitorTask.Status);
            Assert.Equal(PublicationStatus.Processed, DbRequest.RequestStatus);
            Assert.Equal(string.Empty, DbRequest.StatusMessage);
            #endregion
        }

    }
}
