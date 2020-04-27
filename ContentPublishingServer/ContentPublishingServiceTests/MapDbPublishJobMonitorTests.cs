/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContentPublishingLib;
using ContentPublishingLib.JobMonitors;
using TestResourcesLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Turn off all parallelization for the entire assembly
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    public class MapDbPublishJobMonitorTests : IDisposable
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;
        TestInitialization TestResources;

        public MapDbPublishJobMonitorTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
            TestResources = new TestInitialization(_dbLifeTimeFixture.ConnectionString);
            Configuration.ApplicationConfiguration = (ConfigurationRoot)_dbLifeTimeFixture.Configuration;
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task CorrectRequestStatusAfterCancelWhileIdle()
        {
            #region arrange
            MapDbPublishJobMonitor JobMonitor = new MapDbPublishJobMonitor(MapDbPublishJobMonitor.MapDbPublishJobMonitorType.ReducingPublications, TestResources.AuditLogger)
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
            Assert.Contains<TaskStatus>(MonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation } );
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
            Guid ContentItemIdOfThisTest = TestUtil.MakeTestGuid(1);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(1);
            Guid RequestGuid = Guid.NewGuid();

            string ProposedRequestExchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:MapPublishingServerExchangePath"), RequestGuid.ToString());
            string ContentFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:ContentItemRootPath"), ContentItemIdOfThisTest.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("MasterContent", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".qvw");
            string UserGuideFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("UserGuide", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".pdf");

            Directory.CreateDirectory(ProposedRequestExchangeFolder);
            Directory.CreateDirectory(ContentFolder);

            // Copy master content to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, UserGuideFileName),
                      true);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(1));
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
                    FullPath = Path.Combine(ContentFolder, UserGuideFileName),
                    FilePurpose = "UserGuide",
                    Checksum = "e3d450391704b574f010012111af718cf630e444",
                }
            };
            DbRequest.RequestStatus = PublicationStatus.Queued;
            TestResources.DbContext.SaveChanges();

            MapDbPublishJobMonitor JobMonitor = new MapDbPublishJobMonitor(MapDbPublishJobMonitor.MapDbPublishJobMonitorType.NonReducingPublications, TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = new SemaphoreSlim(1, 1),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            DateTime TestStart = DateTime.UtcNow;
            Task MonitorTask = JobMonitor.StartAsync(CancelTokenSource.Token);
            Thread.Sleep(1000);
            Assert.Contains(MonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation } );
            Assert.Equal(PublicationStatus.Queued, DbRequest.RequestStatus);

            while ((DbRequest.RequestStatus == PublicationStatus.Queued ||
                    DbRequest.RequestStatus == PublicationStatus.Processing) &&
                   DateTime.UtcNow - TestStart < new TimeSpan(0,1,0))
            {
                Thread.Sleep(500);
                TestResources.DbContext.Entry(DbRequest).State = EntityState.Detached;
                DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(1));
            }
            #endregion

            #region Assert
            try
            {
                Assert.Contains(MonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation });
                //Assert.Equal(TaskStatus.Running, MonitorTask.Status);
                Assert.Equal(PublicationStatus.PostProcessReady, DbRequest.RequestStatus);
                Assert.Equal(string.Empty, DbRequest.StatusMessage);
                Assert.Equal(0, TestResources.DbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == TestUtil.MakeTestGuid(1)).Count());
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
            Guid ContentItemIdOfThisTest = TestUtil.MakeTestGuid(4);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(4);
            Guid RequestGuid = Guid.NewGuid();

            string ProposedRequestExchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:MapPublishingServerExchangePath"), RequestGuid.ToString());
            string ContentFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("ContentItemRootPath"), ContentItemIdOfThisTest.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("MasterContent", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".qvw");
            string UserGuideFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("UserGuide", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".pdf");

            Directory.CreateDirectory(ProposedRequestExchangeFolder);
            Directory.CreateDirectory(ContentFolder);
            
            // Copy master content to exchange and content folders
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ProposedRequestExchangeFolder, MasterContentFileName),
                      true);
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, UserGuideFileName),
                      true);

            // Modify the request to be tested
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(4));
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
                    FullPath = Path.Combine(ContentFolder, UserGuideFileName),
                    FilePurpose = "UserGuide",
                    Checksum = "e3d450391704b574f010012111af718cf630e444",
                }
            };
            DbRequest.RequestStatus = PublicationStatus.Queued;

            TestResources.DbContext.SaveChanges();

            var queueSemaphore = new SemaphoreSlim(1, 1);

            MapDbPublishJobMonitor PublishJobMonitor = new MapDbPublishJobMonitor(MapDbPublishJobMonitor.MapDbPublishJobMonitorType.ReducingPublications, TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = queueSemaphore,
            };
            MapDbReductionJobMonitor ReductionJobMonitor = new MapDbReductionJobMonitor(TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = queueSemaphore,
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            DateTime TestStart = DateTime.UtcNow;
            Task PublishMonitorTask = PublishJobMonitor.StartAsync(CancelTokenSource.Token);
            Task ReductionMonitorTask = ReductionJobMonitor.StartAsync(CancelTokenSource.Token);
            Thread.Sleep(2000);
            //Assert.Equal(TaskStatus.Running, PublishMonitorTask.Status);
            Assert.Contains(PublishMonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation });

            TestResources.DbContext.Entry(DbRequest).State = EntityState.Detached;
            DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(4));
            Assert.Equal(PublicationStatus.Processing, DbRequest.RequestStatus);

            Thread.Sleep(35000);

            TestResources.DbContext.Entry(DbRequest).State = EntityState.Detached;
            DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(4));
            Assert.Equal(PublicationStatus.Processing, DbRequest.RequestStatus);

            while (DbRequest.RequestStatus == PublicationStatus.Processing &&
                   DateTime.UtcNow - TestStart < new TimeSpan(0, 10, 0))
            {
                Thread.Sleep(500);
                TestResources.DbContext.Entry(DbRequest).State = EntityState.Detached;
                DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == TestUtil.MakeTestGuid(4));
            }
            #endregion

            #region Assert
            var Tasks = TestResources.DbContext.ContentReductionTask
                                          .Where(t => t.ContentPublicationRequestId == TestUtil.MakeTestGuid(4)
                                                   && t.ReductionStatus == ReductionStatusEnum.Reduced)
                                          .ToList();
            try
            {
                //Assert.Equal(TaskStatus.Running, PublishMonitorTask.Status);
                Assert.Contains(PublishMonitorTask.Status, new[] { TaskStatus.Running, TaskStatus.WaitingForActivation });
                Assert.Equal(PublicationStatus.PostProcessReady, DbRequest.RequestStatus);
                Assert.Equal(string.Empty, DbRequest.StatusMessage);
                Assert.Equal(3, Tasks.Count);
                List<ContentReductionTask> NonNullTasks = Tasks.Where(t => t.SelectionGroupId != null).ToList();
                Assert.Equal(2, NonNullTasks.Count);
                Assert.True(File.Exists(NonNullTasks.ElementAt(0).ResultFilePath));
                Assert.True(File.Exists(NonNullTasks.ElementAt(1).ResultFilePath));
            }
            finally
            {
                Directory.Delete(ProposedRequestExchangeFolder, true);
            }
            #endregion
        }

        /// <summary>
        /// Tests that a pub request dated later than a queued reduction task is not taken off its queue to start processing
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PublicationWaitsForEarlierStampedReduction()
        {
            #region Arrange
            Guid ReductionTaskOfThisTest = TestUtil.MakeTestGuid(2);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(3);

            // Modify the reduction task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == ReductionTaskOfThisTest);
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            DbTask.CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0);

            // Modify the publishing request to be tested
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == PubRequestIdOfThisTest);
            DbRequest.RequestStatus = PublicationStatus.Queued;
            DbRequest.CreateDateTimeUtc = DateTime.UtcNow;
            TestResources.DbContext.SaveChanges();

            MapDbPublishJobMonitor TestMonitor = new MapDbPublishJobMonitor(MapDbPublishJobMonitor.MapDbPublishJobMonitorType.ReducingPublications, TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = new SemaphoreSlim(1, 1),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = Task.Run(() => TestMonitor.StartAsync(CancelTokenSource.Token), CancelTokenSource.Token);
            Thread.Sleep(4000);
            try
            {
                CancelTokenSource.Cancel();
                await MonitorTask;
            }
            catch { }
            #endregion

            #region Assert
            // The request should not be taken off the queue because a reduction task is timestamped earlier
            Assert.Equal(PublicationStatus.Queued, DbRequest.RequestStatus);
            #endregion
        }

        /// <summary>
        /// Tests that a pub request dated earlier than all queued reduction tasks is taken off its queue to start processing
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PublicationStartsProcessingBeforeLaterStampedReduction()
        {
            #region Arrange
            Guid ReductionTaskOfThisTest = TestUtil.MakeTestGuid(2);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(3);

            // Modify the reduction task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == ReductionTaskOfThisTest);
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            DbTask.CreateDateTimeUtc = DateTime.UtcNow;

            // Modify the publishing request to be tested
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == PubRequestIdOfThisTest);
            DbRequest.RequestStatus = PublicationStatus.Queued;
            DbRequest.CreateDateTimeUtc = DateTime.UtcNow - new TimeSpan(0, 1, 0);

            TestResources.DbContext.SaveChanges();

            MapDbPublishJobMonitor TestMonitor = new MapDbPublishJobMonitor(MapDbPublishJobMonitor.MapDbPublishJobMonitorType.ReducingPublications, TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = new SemaphoreSlim(1, 1),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task MonitorTask = Task.Run(() => TestMonitor.StartAsync(CancelTokenSource.Token), CancelTokenSource.Token);
            Thread.Sleep(8000);
            try
            {
                CancelTokenSource.Cancel();
                await MonitorTask;
            }
            catch { }

            TestResources.DbContext.Entry(DbRequest).State = EntityState.Detached;
            DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == PubRequestIdOfThisTest);
            #endregion

            #region Assert
            // The request should be taken off the queue because no reduction task is timestamped earlier
            Assert.Contains(DbRequest.RequestStatus, new PublicationStatus[] { PublicationStatus.Processing, PublicationStatus.PostProcessReady });
            #endregion
        }

    }
}
