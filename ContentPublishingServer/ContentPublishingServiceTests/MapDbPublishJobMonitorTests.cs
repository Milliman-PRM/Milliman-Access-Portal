/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.IO;
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
        public void CorrectResultsAfterSuccessfulRun()
        {
            #region arrange
            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 1);
            DbRequest.PublishRequest = new PublishRequest
            {
                RootContentItemId = 1,  // This has DoesReduce = false
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

            string ContentPath = $@"\\indy-syn01\prm_test\ContentRoot\{DbRequest.PublishRequest.RootContentItemId}";
            foreach (var f in Directory.EnumerateFiles(ContentPath))
            {
                //if (f.Contains("Thumbs.db")) continue;
                File.Delete(f);
            }
            #endregion

            #region Act
            Assert.Empty(Directory.EnumerateFiles(ContentPath));
            DateTime TestStart = DateTime.UtcNow;
            Task MonitorTask = JobMonitor.Start(CancelTokenSource.Token);
            Thread.Sleep(1000);
            Assert.Equal(TaskStatus.Running, MonitorTask.Status);
            Assert.Equal(PublicationStatus.Processing, DbRequest.RequestStatus);

            while (DbRequest.RequestStatus == PublicationStatus.Processing &&
                   DateTime.UtcNow - TestStart < new TimeSpan(0,1,0))
            {
                Thread.Sleep(500);
            }
            #endregion

            #region Assert again
            Assert.Equal(TaskStatus.Running, MonitorTask.Status);
            Assert.Equal(PublicationStatus.Processed, DbRequest.RequestStatus);
            Assert.Equal(string.Empty, DbRequest.StatusMessage);
            Assert.True(File.Exists(Path.Combine(ContentPath, "MasterContent.Pub[1].Content[1].qvw")));
            Assert.True(File.Exists(Path.Combine(ContentPath, "Thumbnail.Pub[1].Content[1].jpg")));
            #endregion
        }

    }
}
