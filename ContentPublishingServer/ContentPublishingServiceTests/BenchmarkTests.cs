using ContentPublishingLib;
using ContentPublishingLib.JobMonitors;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestResourcesLib;
using Xunit;
using Xunit.Abstractions;
using ContentPublishingLib.JobRunners;

namespace ContentPublishingServiceTests
{
    [Collection("DatabaseLifetime collection")]
    [LogTestBeginEnd]
    public class BenchmarkTests
    {
        private readonly ITestOutputHelper _output;
        DatabaseLifetimeFixture _dbLifeTimeFixture;
        TestInitialization TestResources;

        public BenchmarkTests(ITestOutputHelper output, DatabaseLifetimeFixture dbLifeTimeFixture)
        {
            _output = output;
            _dbLifeTimeFixture = dbLifeTimeFixture;
            Configuration.ApplicationConfiguration = (ConfigurationRoot)_dbLifeTimeFixture.Configuration;
            TestResources = new TestInitialization(_dbLifeTimeFixture.ConnectionString, Configuration.ApplicationConfiguration);
        }

        [Fact(Skip = "Local only")]
        public void ConcurrentReductions()
        {
            const int MAX_CONCURRENT_TASK_CAP = 4;
            const int TOTAL_TASKS = 100;
            const int MINUTES_PER_TASK = 3;

            var smallDbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 1)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.Values.Count == 2
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 0434")
                                                                   && f.Values.Exists(v => v.Value == "Assigned Provider Clinic (Hier) 4025")))
                .Single();
            var largeDbTask = TestResources.DbContext.ContentReductionTask
                .AsEnumerable()
                .Where(t => t.SelectionCriteriaObj.Fields.Count == 3)
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.FieldName == "Population" 
                                                                   && f.Values.Count == 1))
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.FieldName == "Practice"
                                                                   && f.Values.Count == 108))
                .Where(t => t.SelectionCriteriaObj.Fields.Exists(f => f.FieldName == "Provider"
                                                                   && f.Values.Count == 658))
                .Single();

            // Use a fact with nested for loops instead of a theory
            // This guarantees the order in which these serial tests run and gathers output into one test
            for (int maxConcurrentTasks = 1; maxConcurrentTasks <= MAX_CONCURRENT_TASK_CAP; maxConcurrentTasks += 1)
            {
                #region Arrange
                var dbTasks = new List<ContentReductionTask>();
                var exchangeFolderList = new List<string>();

                for (int taskNo = 0; taskNo < TOTAL_TASKS; taskNo += 1)
                {
                    var dbTask = (taskNo % 2 == 0) ? smallDbTask : largeDbTask;
                    var taskGuid = Guid.NewGuid();

                    string exchangeFolder = Path.Combine(_dbLifeTimeFixture.Configuration.GetValue<string>("Storage:MapPublishingServerExchangePath"), taskGuid.ToString());
                    string masterContentFileName = ContentTypeSpecificApiBase.GenerateContentFileName(
                        "MasterContent", $".{taskNo}.qvw", dbTask.SelectionGroup.RootContentItemId);
                    string masterContentFilePath = Path.Combine(exchangeFolder, masterContentFileName);

                    Directory.CreateDirectory(exchangeFolder);
                    File.Copy(dbTask.MasterFilePath, masterContentFilePath, true);

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
                        MasterFilePath = masterContentFilePath,
                        MasterContentChecksum = dbTask.MasterContentChecksum,
                        SelectionCriteriaObj = dbTask.SelectionCriteriaObj,
                    });
                }
                TestResources.DbContext.ContentReductionTask.AddRange(dbTasks);
                TestResources.DbContext.SaveChanges();

                var jobMonitor = new MapDbReductionJobMonitor(TestResources.AuditLogger)
                {
                    ConnectionString = _dbLifeTimeFixture.ConnectionString,
                    MaxConcurrentRunners = maxConcurrentTasks,
                };

                var cancelTokenSource = new CancellationTokenSource();
                #endregion

                #region Act
                // Placing all asserts within the try block means any failed assertion
                // will still clean up the exchange folders
                TimeSpan reductionTime = TimeSpan.Zero;
                var errors = new List<string>();
                try
                {
                    Assert.All(dbTasks, r => Assert.Equal(ReductionStatusEnum.Queued, r.ReductionStatus));

                    var testStart = DateTime.UtcNow;
                    var monitorTask = jobMonitor.StartAsync(cancelTokenSource.Token);
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
                    Assert.All(dbTasks, r => Assert.Contains(r.ReductionStatus, new List<ReductionStatusEnum>
                    {
                        ReductionStatusEnum.Reduced,
                        ReductionStatusEnum.Error,
                    }));

                    try
                    {
                        cancelTokenSource.Cancel();
                        monitorTask.Wait();
                    }
                    catch (AggregateException e)
                    {
                        if (!(e.InnerException is OperationCanceledException))
                        {
                            throw;
                        }
                    }
                    Assert.True(monitorTask.IsCanceled);
                }
                finally
                {
                    foreach (var exchangeFolder in exchangeFolderList)
                    {
                        Directory.Delete(exchangeFolder, true);
                    }
                }

                errors.AddRange(dbTasks.Select(t => t.ReductionStatusMessage).Where(m => m != ""));

                var timePerTask = reductionTime / TOTAL_TASKS;
                const string FORMAT = "m\\:ss\\.fff";
                _output.WriteLine(string.Format(
                    "[Max threads = {0}] {1} tasks completed in {2} ({3} per task) with {4} errored reductions.",
                    maxConcurrentTasks,
                    TOTAL_TASKS,
                    reductionTime.ToString(FORMAT),
                    timePerTask.ToString(FORMAT),
                    errors.Count));
                foreach (var error in errors)
                {
                    _output.WriteLine(error);
                }
                #endregion
            }
        }
    }
}
