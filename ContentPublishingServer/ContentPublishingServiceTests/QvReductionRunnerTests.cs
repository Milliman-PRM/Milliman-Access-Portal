/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Test cases to evaluate QvReductionRunner features. 
 * DEVELOPER NOTES: Depends on an actual Qlikview Publisher to perform content reduction. 
 */

using ContentPublishingLib;
using ContentPublishingLib.JobRunners;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;

namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
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
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 1)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Count == 2
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 0434")
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 4025")))
                .Single();

            string ExchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:MapPublishingServerExchangePath"), DbTask.Id.ToString());
            string MasterContentFileName = ContentTypeSpecificApiBase.GenerateContentFileName("MasterContent", ".qvw", DbTask.SelectionGroup.RootContentItemId);

            Directory.CreateDirectory(ExchangeFolder);
            File.Copy(@"\\indy-qlikview\testing\Sample Data\CCR_0273ZDM_New_Reduction_Script.qvw",
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
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 1)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Count == 1
                                                                   && f.Values.Exists(v => v.Value == "Invalid value")))
                .Single();

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
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 1)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Count == 1
                                                                   && f.Values.Exists(v => v.Value == "Invalid selection value")))
                .Single();

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
            // Query for the task to be used in this test
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask
                                   .AsEnumerable()
                                   .Where(t => t.SelectionCriteriaObj.Fields.Count == 2)
                                   .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Exists(v => v.Value == "Invalid selection value")))
                                   .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 4025")))
                                   .Single();

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
            DbTask = TestResources.DbContext.ContentReductionTask.Find(DbTask.Id);
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

            Assert.Equal($@"\\indy-qlikview\testing\Sample Data\Test1\{ContentTypeSpecificApiBase.GenerateReducedContentFileName(DbTask.SelectionGroupId.Value, DbTask.SelectionGroup.RootContentItemId, ".qvw")}" ,TaskResult.ReducedContentFilePath);
            Assert.Equal(40, TaskResult.ReducedContentFileChecksum.Length);
            Assert.True(File.Exists(TaskResult.ReducedContentFilePath));
            #endregion
        }

        [Fact]
        public async Task MasterFileMissing()
        {
            #region Arrange
            // Modify the task to be tested
            ContentReductionTask DbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 1)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Count == 2
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 0434")
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 4025")))
                .Single();

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
