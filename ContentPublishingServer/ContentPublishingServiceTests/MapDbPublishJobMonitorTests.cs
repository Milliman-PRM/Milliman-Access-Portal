/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
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

// Turn off all parallelization for the entire assembly
[assembly: CollectionBehavior(DisableTestParallelization = true)]
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
        public void CorrectResultsAfterSuccessfulRunDoesReduceFalse()
        {
            #region arrange
            long ContentItemIdOfThisTest = 1;
            long PubRequestIdOfThisTest = 1;
            Guid RequestGuid = Guid.NewGuid();

            string ProposedRequestExchangeFolder = $@"\\indy-syn01\prm_test\MapPublishingServerExchange\{RequestGuid}\";
            string ContentFolder = $@"\\indy-syn01\prm_test\ContentRoot\{ContentItemIdOfThisTest}";
            string MasterContentFileName = $"MasterContent.Pub[{ContentItemIdOfThisTest}].Content[{PubRequestIdOfThisTest}].qvw";

            Directory.CreateDirectory(ProposedRequestExchangeFolder);
            Directory.CreateDirectory(ContentFolder);

            // Copy master content to content folder
            File.Copy(@"\\indy-syn01\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-syn01\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, $"UserGuide.Pub[{PubRequestIdOfThisTest}].Content[{ContentItemIdOfThisTest}].pdf"),
                      true);

            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 1);
            DbRequest.LiveReadyFilesObj = new List<ContentRelatedFile>
            {
                new ContentRelatedFile
                {
                    FullPath = Path.Combine(ContentFolder, MasterContentFileName),
                    FilePurpose = "MasterContent",
                    Checksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                },
                new ContentRelatedFile
                {
                    FullPath = Path.Combine(ContentFolder, $"UserGuide.Pub[{PubRequestIdOfThisTest}].Content[{ContentItemIdOfThisTest}].pdf"),
                    FilePurpose = "UserGuide",
                    Checksum = "e3d450391704b574f010012111af718cf630e444",
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

            #region Assert
            try
            {
                Assert.Equal(TaskStatus.Running, MonitorTask.Status);
                Assert.Equal(PublicationStatus.Processed, DbRequest.RequestStatus);
                Assert.Equal(string.Empty, DbRequest.StatusMessage);
                Assert.Equal(0, MockContext.Object.ContentReductionTask.Where(t => t.ContentPublicationRequestId == 1).Count());
            }
            finally
            {
                Directory.Delete(ProposedRequestExchangeFolder, true);
            }
            #endregion
        }

        /// <summary>
        /// This test represents end to end publication for an existing content item (ID=4) that has 2 reducing selection groups
        /// </summary>
        [Fact]
        public void CorrectResultsAfterSuccessfulRunDoesReduceTrue()
        {
            #region arrange
            long ContentItemIdOfThisTest = 4;
            long PubRequestIdOfThisTest = 4;
            Guid RequestGuid = Guid.NewGuid();

            string ProposedRequestExchangeFolder = $@"\\indy-syn01\prm_test\MapPublishingServerExchange\{RequestGuid}\";
            string ContentFolder = $@"\\indy-syn01\prm_test\ContentRoot\{ContentItemIdOfThisTest}";
            string MasterContentFileName = $"MasterContent.Pub[{ContentItemIdOfThisTest}].Content[{PubRequestIdOfThisTest}].qvw";

            Directory.CreateDirectory(ProposedRequestExchangeFolder);
            Directory.CreateDirectory(ContentFolder);
            
            // Copy master content to exchange and content folders
            File.Copy(@"\\indy-syn01\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ProposedRequestExchangeFolder, MasterContentFileName),
                      true);
            File.Copy(@"\\indy-syn01\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-syn01\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, $"UserGuide.Pub[{PubRequestIdOfThisTest}].Content[{ContentItemIdOfThisTest}].pdf"),
                      true);

            Mock<ApplicationDbContext> MockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = MockContext.Object.ContentPublicationRequest.Single(t => t.Id == 4);
            DbRequest.ReductionRelatedFilesObj = new List<ReductionRelatedFiles>
            {
                new ReductionRelatedFiles
                {
                    MasterContentFile = new ContentRelatedFile
                    {
                        FullPath = Path.Combine(ProposedRequestExchangeFolder, MasterContentFileName),
                        FilePurpose = "MasterContent",
                        Checksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                    },
                    ReducedContentFileList = new List<ContentRelatedFile>(),
                }
            };
            DbRequest.LiveReadyFilesObj = new List<ContentRelatedFile>
            {
                new ContentRelatedFile
                {
                    FullPath = Path.Combine(ContentFolder, MasterContentFileName),
                    FilePurpose = "MasterContent",
                    Checksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B",
                },
                new ContentRelatedFile
                {
                    FullPath = Path.Combine(ContentFolder, $"UserGuide.Pub[{PubRequestIdOfThisTest}].Content[{ContentItemIdOfThisTest}].pdf"),
                    FilePurpose = "UserGuide",
                    Checksum = "e3d450391704b574f010012111af718cf630e444",
                }
            };
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishJobMonitor PublishJobMonitor = new MapDbPublishJobMonitor
            {
                MockContext = MockContext,
            };
            MapDbReductionJobMonitor ReductionJobMonitor = new MapDbReductionJobMonitor
            {
                MockContext = MockContext,
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            DateTime TestStart = DateTime.UtcNow;
            Task PublishMonitorTask = PublishJobMonitor.Start(CancelTokenSource.Token);
            Task ReductionMonitorTask = ReductionJobMonitor.Start(CancelTokenSource.Token);
            Thread.Sleep(2000);
            Assert.Equal(TaskStatus.Running, PublishMonitorTask.Status);
            Assert.Equal(PublicationStatus.Processing, DbRequest.RequestStatus);

            while (DbRequest.RequestStatus == PublicationStatus.Processing &&
                   DateTime.UtcNow - TestStart < new TimeSpan(0, 10, 0))
            {
                Thread.Sleep(500);
            }
            #endregion

            #region Assert
            var Tasks = MockContext.Object.ContentReductionTask
                                          .Where(t => t.ContentPublicationRequestId == 4
                                                   && t.ReductionStatus == ReductionStatusEnum.Reduced)
                                          .ToList();
            try
            {
                Assert.Equal(TaskStatus.Running, PublishMonitorTask.Status);
                Assert.Equal(PublicationStatus.Processed, DbRequest.RequestStatus);
                Assert.Equal(string.Empty, DbRequest.StatusMessage);
                Assert.Equal(2, Tasks.Count);
                Assert.True(File.Exists(Tasks.ElementAt(0).ResultFilePath));
                Assert.True(File.Exists(Tasks.ElementAt(1).ResultFilePath));
            }
            finally
            {
                Directory.Delete(ProposedRequestExchangeFolder, true);
            }
            #endregion
        }

    }
}
