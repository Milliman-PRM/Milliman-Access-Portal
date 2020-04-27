/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Test cases to evaluate QvReductionRunner features. 
 * DEVELOPER NOTES: Depends on an actual Qlikview Publisher to perform content reduction. 
 */

using ContentPublishingLib;
using ContentPublishingLib.JobRunners;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    public class QvReductionRunnerTests
    {
        DatabaseLifetimeFixture _dbLifeTimeFixture;
        TestInitialization TestResources;

        public QvReductionRunnerTests(DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _dbLifeTimeFixture = dbLifeTimeFixture;
            TestResources = new TestInitialization(_dbLifeTimeFixture.ConnectionString);
            Configuration.ApplicationConfiguration = (ConfigurationRoot)_dbLifeTimeFixture.Configuration;
        }

        [Fact]
        public async Task SuccessfulHierarchyOnly()
        {
            #region Arrange
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(1));

            string ExchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("MapPublishingServerExchangePath"), DbTask.Id.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GenerateContentFileName("MasterContent", ".qvw", DbTask.SelectionGroup.RootContentItemId);

            Directory.CreateDirectory(ExchangeFolder);
            File.Copy(@"\\indy-srv-02\prm_test\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
                      Path.Combine(ExchangeFolder, MasterContentFileName),
                      true);

            // Modify the task to be tested
            DbTask.TaskAction = TaskActionEnum.HierarchyOnly;
            DbTask.MasterFilePath = Path.Combine(ExchangeFolder, MasterContentFileName);
            DbTask.MasterContentChecksum = "1412C93D02FE7D2AF6F0146B772FB78E6455537B";
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            TestResources.DbContext.SaveChanges();

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            ReductionJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            #endregion

            #region Assert
            try
            {
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
                Assert.Equal(ReductionJobDetail.JobStatusEnum.Success, JobDetail.Status);

                Assert.NotNull(TaskResult.MasterContentHierarchy);
                Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
                Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
                Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
                Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

                Assert.Null(TaskResult.ReducedContentHierarchy);
                Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
                Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            }
            finally
            {
                Directory.Delete(ExchangeFolder, true);
            }
            #endregion
        }

        [Fact]
        public async Task InvalidSelectionFieldName()
        {
            #region Arrange
            // Modify the task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(4));
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            TestResources.DbContext.SaveChanges();

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            ReductionJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Error, JobDetail.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal to test for 1 == count gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);

            Assert.Null(TaskResult.ReducedContentHierarchy);

            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));

            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Matches("(The requested reduction field).*(is not found in the reduction hierarchy)", TaskResult.StatusMessage);
            #endregion
        }

        [Fact]
        public async Task InvalidSelectionValue()
        {
            #region Arrange
            // Modify the task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(2));
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            TestResources.DbContext.SaveChanges();

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            ReductionJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Warning, JobDetail.Status);

            Assert.NotNull(TaskResult.MasterContentHierarchy);
            Assert.Equal(3, TaskResult.MasterContentHierarchy.Fields.Count);
            Assert.Single(TaskResult.MasterContentHierarchy.Fields[0].FieldValues); // using .Equal for expected value 1 gives compiler warning
            Assert.Equal(7, TaskResult.MasterContentHierarchy.Fields[1].FieldValues.Count);
            Assert.Equal(54, TaskResult.MasterContentHierarchy.Fields[2].FieldValues.Count);
            Assert.Null(TaskResult.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Contains("None of the 1 specified selections exist in the master content hierarchy", TaskResult.StatusMessage);
            #endregion
        }

        [Fact]
        public async Task OneValidOneInvalidSelectionValue()
        {
            #region Arrange
            // Modify the task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(3));
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            TestResources.DbContext.SaveChanges();

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            ReductionJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;

            TestResources.DbContext.Entry(DbTask).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(3));
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
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Success, JobDetail.Status);

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

            Assert.Equal($@"\\indy-srv-02\prm_test\Sample Data\Test1\{ContentTypeSpecificApiBase.GenerateReducedContentFileName(DbTask.SelectionGroupId.Value, DbTask.SelectionGroup.RootContentItemId, ".qvw")}" ,TaskResult.ReducedContentFilePath);
            Assert.Equal(40, TaskResult.ReducedContentFileChecksum.Length);
            Assert.True(File.Exists(TaskResult.ReducedContentFilePath));
            #endregion
        }

        [Fact]
        public async Task MasterFileMissing()
        {
            #region Arrange
            // Modify the task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(1));
            DbTask.ReductionStatus = ReductionStatusEnum.Queued;
            DbTask.MasterFilePath = Path.ChangeExtension(DbTask.MasterFilePath, "xyz");
            TestResources.DbContext.SaveChanges();

            QvReductionRunner TestRunner = new QvReductionRunner
            {
                JobDetail = (ReductionJobDetail)DbTask,
            };
            TestRunner.SetTestAuditLogger(TestResources.AuditLogger);

            CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
            #endregion

            #region Act
            Task<ReductionJobDetail> MonitorTask = TestRunner.Execute(CancelTokenSource.Token);
            ReductionJobDetail JobDetail = await MonitorTask;
            var TaskResult = JobDetail.Result;
            var TaskRequest = JobDetail.Request;
            #endregion

            #region Assert
            Assert.NotNull(TaskRequest);
            Assert.NotNull(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobResult>(TaskResult);
            Assert.IsType<ReductionJobDetail.ReductionJobRequest>(TaskRequest);
            Assert.Equal<TaskStatus>(TaskStatus.RanToCompletion, MonitorTask.Status);
            Assert.False(MonitorTask.IsCanceled);
            Assert.False(MonitorTask.IsFaulted);
            Assert.Equal(ReductionJobDetail.JobStatusEnum.Error, JobDetail.Status);
            Assert.Null(TaskResult.MasterContentHierarchy);
            Assert.Null(TaskResult.ReducedContentHierarchy);
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFilePath));
            Assert.True(string.IsNullOrWhiteSpace(TaskResult.ReducedContentFileChecksum));
            Assert.Matches("(Master file).*(does not exist)", TaskResult.StatusMessage);
            #endregion
        }
    }
}
