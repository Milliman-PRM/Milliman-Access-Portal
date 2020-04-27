/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using ContentPublishingLib;
using ContentPublishingLib.JobMonitors;
using ContentPublishingLib.JobRunners;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    public class MapDbPublishRunnerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;
        TestInitialization TestResources;

        public MapDbPublishRunnerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
            TestResources = new TestInitialization(_dbLifeTimeFixture.ConnectionString);
            Configuration.ApplicationConfiguration = (ConfigurationRoot)_dbLifeTimeFixture.Configuration;
        }

        /// <summary>
        /// Tests that a pub request for a content item with DoesReduce == false and 1 
        /// master selection group, succeeds and creates no reduction related artifacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DoesNotReduce_MasterSelectionGroupExists()
        {
            #region Arrange
            Guid ContentItemIdOfThisTest = TestUtil.MakeTestGuid(1);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(1);

            string ContentFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:ContentItemRootPath"), ContentItemIdOfThisTest.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("MasterContent", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".qvw");
            string UserGuideFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("UserGuide", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".pdf");

            Directory.CreateDirectory(ContentFolder);

            // Copy master content to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, UserGuideFileName),
                      true);

            // Modify the task to be tested
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

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                JobDetail = PublishJobDetail.New(DbRequest),
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Equal(1, TestResources.DbContext.SelectionGroup.Count(g => g.RootContentItemId == TestUtil.MakeTestGuid(1)));  // check before
            #endregion

            #region Act
            Task<PublishJobDetail> MonitorTask = Task.Run(() => TestRunner.Execute(CancelTokenSource.Token), CancelTokenSource.Token);
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
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, JobDetail.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.ResultingRelatedFiles);
            Assert.Empty(TaskResult.ResultingRelatedFiles);
            Assert.Equal(1, TestResources.DbContext.SelectionGroup.Count(g => g.RootContentItemId == TestUtil.MakeTestGuid(1)));  // check after
            Assert.True(TestResources.DbContext.SelectionGroup.Single(g => g.RootContentItemId == TestUtil.MakeTestGuid(1)).IsMaster);
            Assert.Empty(TestResources.DbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == DbRequest.Id));
            #endregion
        }

        /// <summary>
        /// Tests that a pub request for a content item with DoesReduce == false and no selection 
        /// groups causes creation of a new master selection group and no reduction related artifacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DoesNotReduce_NoSelectionGroupExists()
        {
            #region Arrange
            Guid ContentItemIdOfThisTest = TestUtil.MakeTestGuid(2);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(2);

            string ContentFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:ContentItemRootPath"), ContentItemIdOfThisTest.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("MasterContent", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".qvw");
            string UserGuideFileName = ContentTypeSpecificApiBase.GeneratePreliveRelatedFileName("UserGuide", PubRequestIdOfThisTest, ContentItemIdOfThisTest, ".pdf");

            Directory.CreateDirectory(ContentFolder);

            // Copy master content to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ContentFolder, MasterContentFileName),
                      true);
            // Copy related file to content folder
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\IHopeSo.pdf",
                      Path.Combine(ContentFolder, UserGuideFileName),
                      true);


            // Modify the task to be tested
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == PubRequestIdOfThisTest);
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
            DbRequest.ReductionRelatedFilesObj = new List<ReductionRelatedFiles>
            {
                new ReductionRelatedFiles
                {
                    MasterContentFile = DbRequest.LiveReadyFilesObj.Single(f => f.FilePurpose == "MasterContent")
                }
            };
            DbRequest.RequestStatus = PublicationStatus.Queued;

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                JobDetail = PublishJobDetail.New(DbRequest),
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Empty(TestResources.DbContext.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId));  // check before
            #endregion

            #region Act
            Task<PublishJobDetail> MonitorTask = Task.Run(() => TestRunner.Execute(CancelTokenSource.Token), CancelTokenSource.Token);
            PublishJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            List<SelectionGroup> AllSelGroups = TestResources.DbContext.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId).ToList();
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
            Assert.Equal(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(PublishJobDetail.JobStatusEnum.Success, JobDetail.Status);

            Assert.Equal(string.Empty, TaskResult.StatusMessage);
            Assert.NotNull(TaskResult.ResultingRelatedFiles);
            Assert.Empty(TaskResult.ResultingRelatedFiles);
            Assert.Single(AllSelGroups);
            Assert.True(AllSelGroups.Single().IsMaster);
            Assert.Empty(TestResources.DbContext.ContentReductionTask.Where(t => t.ContentPublicationRequestId == DbRequest.Id));
            #endregion
        }

        [Fact]
        public async Task DoesReduce_NoSelectionGroupExistsDoesReduceTrue()
        {
            #region Arrange
            Guid ContentItemIdOfThisTest = TestUtil.MakeTestGuid(3);
            Guid PubRequestIdOfThisTest = TestUtil.MakeTestGuid(3);
            Guid RequestGuid = Guid.NewGuid();

            string ProposedRequestExchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:MapPublishingServerExchangePath"), RequestGuid.ToString());
            string ContentFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:ContentItemRootPath"), ContentItemIdOfThisTest.ToString());
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
            ContentPublicationRequest DbRequest = TestResources.DbContext.ContentPublicationRequest.Single(t => t.Id == PubRequestIdOfThisTest);
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

            MapDbPublishRunner TestRunner = new MapDbPublishRunner
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                JobDetail = PublishJobDetail.New(DbRequest),
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            MapDbReductionJobMonitor ReductionMonitor = new MapDbReductionJobMonitor(TestResources.AuditLogger)
            {
                ConnectionString = _dbLifeTimeFixture.ConnectionString,
                QueueSemaphore = new SemaphoreSlim(1, 1),
            };

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            Assert.Empty(TestResources.DbContext.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId));  // check before

            Task TaskMonitorTask = ReductionMonitor.StartAsync(CancelTokenSource.Token);
            #endregion

            #region Act
            Task<PublishJobDetail> RequestRunnerTask = TestRunner.Execute(CancelTokenSource.Token);
            PublishJobDetail JobDetail = await RequestRunnerTask;

            CancelTokenSource.Cancel();  // End the MapDbReductionJobMonitor

            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            List<SelectionGroup> AllSelGroups = TestResources.DbContext.SelectionGroup.Where(g => g.RootContentItemId == DbRequest.RootContentItemId).ToList();
            #endregion

            #region Assert
            try
            {
                Assert.NotNull(TaskRequest);
                Assert.NotNull(TaskResult);
                Assert.IsType<PublishJobDetail.PublishJobResult>(TaskResult);
                Assert.Equal(TaskStatus.RanToCompletion, RequestRunnerTask.Status);
                Assert.False(RequestRunnerTask.IsCanceled);
                Assert.False(RequestRunnerTask.IsFaulted);
                Assert.Equal(PublishJobDetail.JobStatusEnum.Success, JobDetail.Status);

                Assert.Equal(string.Empty, TaskResult.StatusMessage);
                Assert.NotNull(TaskResult.ResultingRelatedFiles);
                Assert.Empty(TaskResult.ResultingRelatedFiles);
                Assert.Single(AllSelGroups);
                Assert.True(AllSelGroups.Single().IsMaster);
                // look at the generated reductiontask, assert it has master details and no reduction details.
            }
            finally
            {
                Directory.Delete(ProposedRequestExchangeFolder, true);
            }
            #endregion
        }

    }
}
