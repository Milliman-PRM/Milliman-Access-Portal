using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ContentPublishingLib.JobMonitors;
using TestResourcesLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Moq;
using Xunit.Abstractions;
using ContentPublishingLib.JobRunners;

namespace ContentPublishingServiceTests
{
    public class Benchmarks : ContentPublishingServiceTestBase
    {
        private readonly ITestOutputHelper _output;

        public Benchmarks(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Local only")]
        public void ConcurrentReductions()
        {
            const int MAX_CONCURRENT_TASK_CAP = 1;
            const int TOTAL_TASKS = 1;
            const int MINUTES_PER_TASK = 10;

            var mockContext = MockMapDbContext.New(InitializeTests.InitializeWithUnspecifiedStatus);
            var dbTask = mockContext.Object.ContentReductionTask.Single(t => t.Id == TestUtil.MakeTestGuid(1));

            // Use a fact with nested for loops instead of a theory
            // This guarantees the order in which these serial tests run and gathers output into one test
            for (int maxConcurrentTasks = 1; maxConcurrentTasks <= MAX_CONCURRENT_TASK_CAP; maxConcurrentTasks += 1)
            {
                #region Arrange
                var dbTasks = new List<ContentReductionTask>();
                var exchangeFolderList = new List<string>();

                for (int taskNo = 0; taskNo < TOTAL_TASKS; taskNo += 1)
                {
                    var taskGuid = Guid.NewGuid();

                    string exchangeFolder = $@"\\indy-syn01\prm_test\MapPublishingServerExchange\{taskGuid}\";
                    string masterContentFileName = ContentTypeSpecificApiBase.GenerateContentFileName(
                        "MasterContent", ".qvw", dbTask.SelectionGroup.RootContentItemId);

                    Directory.CreateDirectory(exchangeFolder);
                    File.Copy(dbTask.MasterFilePath, Path.Combine(exchangeFolder, masterContentFileName), true);

                    exchangeFolderList.Add(exchangeFolder);

                    // Modify the task to be tested
                    // This allows the task caps to be changed without defining more tasks in test initialization
                    dbTasks.Add(new ContentReductionTask
                    {
                        Id = taskGuid,
                        TaskAction = dbTask.TaskAction,
                        ReductionStatus = ReductionStatusEnum.Queued,
                        CreateDateTimeUtc = dbTask.CreateDateTimeUtc,
                        SelectionGroupId = dbTask.SelectionGroupId,
                        MasterFilePath = dbTask.MasterFilePath,
                        MasterContentChecksum = dbTask.MasterContentChecksum,
                        SelectionCriteriaObj = dbTask.SelectionCriteriaObj,
                    });
                }
                mockContext.Object.ContentReductionTask.AddRange(dbTasks);
                MockDbSet<ContentReductionTask>.AssignNavigationProperty(mockContext.Object.ContentReductionTask, "SelectionGroupId", mockContext.Object.SelectionGroup);

                var jobMonitor = new MapDbReductionJobMonitor
                {
                    MockContext = mockContext,
                };

                var cancelTokenSource = new CancellationTokenSource();
                #endregion

                #region Act
                // Placing all asserts within the try block means any failed assertion
                // will still clean up the exchange folders
                TimeSpan reductionTime = TimeSpan.Zero;
                try
                {
                    Assert.All(dbTasks, r => Assert.Equal(ReductionStatusEnum.Queued, r.ReductionStatus));

                    var testStart = DateTime.UtcNow;
                    var monitorTask = jobMonitor.Start(cancelTokenSource.Token);
                    Thread.Sleep(1000);
                    Assert.Equal(TaskStatus.Running, monitorTask.Status);

                    while (dbTasks.Any(r => r.ReductionStatus == ReductionStatusEnum.Queued
                                         || r.ReductionStatus == ReductionStatusEnum.Reducing))
                    {
                        Assert.True(DateTime.UtcNow - testStart < new TimeSpan(0, TOTAL_TASKS * MINUTES_PER_TASK, 0),
                            $"Reduction timed out (Max threads = {maxConcurrentTasks}, tasks = {TOTAL_TASKS})");
                        Thread.Sleep(500);
                    }
                    reductionTime = DateTime.UtcNow - testStart;
                #endregion

                #region Assert
                    Assert.Equal(TaskStatus.Running, monitorTask.Status);
                    Assert.All(dbTasks, r => Assert.Equal(ReductionStatusEnum.Reduced, r.ReductionStatus));
                    Assert.All(dbTasks, r => Assert.Equal(string.Empty, r.ReductionStatusMessage));
                }
                finally
                {
                    foreach (var exchangeFolder in exchangeFolderList)
                    {
                        Directory.Delete(exchangeFolder, true);
                    }
                }

                var timePerTask = reductionTime / TOTAL_TASKS;
                const string FORMAT = "m\\:ss\\.fff";
                _output.WriteLine(string.Format(
                    "[Max threads = {0}] {1} tasks completed in {2} ({3} per task)",
                    maxConcurrentTasks,
                    TOTAL_TASKS,
                    reductionTime.ToString(FORMAT),
                    timePerTask.ToString(FORMAT)));
                #endregion
            }
        }
    }
}
