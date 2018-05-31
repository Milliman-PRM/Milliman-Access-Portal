/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using AuditLogLib;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Moq;

namespace ContentPublishingLib.JobRunners
{
    public class MapDbPublishRunner : RunnerBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
        private string _ThisRootContentFolder = string.Empty;
        string _ContentItemRootPath = string.Empty;

        private object AuditLogDetailObj;
        private AuditEvent AuditLogEvent;

        public PublishJobDetail JobDetail { get; set; } = new PublishJobDetail();

        /// <summary>
        /// Initializes data used to construct database context instances using a named configuration parameter.
        /// </summary>
        internal string ConfiguredConnectionStringParamName
        {
            set
            {
                ConnectionString = Configuration.GetConnectionString(value);
            }
        }

        internal TimeSpan TimeLimit { get; set; } = new TimeSpan(6, 0, 0);  // TODO Get the timeout from configuration)

        /// <summary>
        /// Initializes data used to construct database context instances.
        /// </summary>
        internal string ConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                ContextOptions = ContextBuilder.Options;
            }
        }

        private Mock<ApplicationDbContext> _MockContext = null;
        public Mock<ApplicationDbContext> MockContext
        {
            protected get
            {
                return _MockContext;
            }
            set
            {
                AssertTesting();
                _MockContext = value;
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        public MapDbPublishRunner()
        {
            _ContentItemRootPath = Configuration.ApplicationConfiguration.GetSection("Storage")["ContentItemRootPath"];
            if (!Directory.Exists(_ContentItemRootPath))
            {
                throw new ApplicationException($"Configured Storage:ContentItemRootPath folder <{_ContentItemRootPath}> does not exist");
            }
        }

        /// <summary>
        /// Gets an appropriate ApplicationDbContext object, depending on whether a Mocked context is assigned (test run)
        /// </summary>
        /// <returns></returns>
        protected ApplicationDbContext GetDbContext()
        {
            return MockContext != null
                 ? MockContext.Object
                 : new ApplicationDbContext(ContextOptions);
        }

        /// <summary>
        /// Main functional entry point for the runner, 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PublishJobDetail> Execute(CancellationToken cancellationToken)
        {
            MethodBase Method = MethodBase.GetCurrentMethod();
            DateTime WaitEndUtc = DateTime.UtcNow + TimeLimit;

            _CancellationToken = cancellationToken;
            // AuditLog would not be null during a test run where it may be initialized earlier
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            _ThisRootContentFolder = Path.Combine(_ContentItemRootPath, JobDetail.Request.RootContentId.ToString());
            DirectoryInfo ContentDirectoryInfo = Directory.CreateDirectory(_ThisRootContentFolder);

            try
            {
                // Handle each file related to this PublicationRequest
                foreach (ContentRelatedFile RelatedFile in JobDetail.Request.RelatedFiles)
                {
                    HandleRelatedFile(RelatedFile);
                }

                // Wait for any/all related reduction tasks to complete
                int PendingTaskCount = 0;
                do
                {
                    PendingTaskCount = await CheckRelatedReductionTasks();

                    Thread.Sleep(1000);

                    if (DateTime.UtcNow > WaitEndUtc)
                    {
                        throw new ApplicationException($"{Method.DeclaringType.Name}.{Method.Name} timed out waiting for {PendingTaskCount} pending reduction tasks");
                    }
                }
                while (PendingTaskCount > 0); 

                JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;
            }
            catch (OperationCanceledException e)
            {
                string msg = GlobalFunctions.LoggableExceptionString(e);
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {msg}");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Canceled;
                JobDetail.Result.StatusMessage = msg;
            }
            catch (Exception e)
            {
                string msg = GlobalFunctions.LoggableExceptionString(e);
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {msg}");
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                JobDetail.Result.StatusMessage = msg;
            }

            return JobDetail;
        }

        private void HandleRelatedFile(ContentRelatedFile RelatedFile)
        {
            if (!File.Exists(RelatedFile.FullPath))
            {
                throw new ApplicationException($"While publishing request {JobDetail.JobId.ToString()}, uploaded file not found at path [{RelatedFile.FullPath}].");
            }
            if (RelatedFile.Checksum.ToLower() != GlobalFunctions.GetFileChecksum(RelatedFile.FullPath).ToLower())
            {
                throw new ApplicationException($"While publishing request {JobDetail.JobId.ToString()}, checksum validation failed for file [{RelatedFile.FullPath}].");
            }

            string DestinationFileName = $"{RelatedFile.FilePurpose}.Pub[{JobDetail.JobId.ToString()}].Content[{JobDetail.Request.RootContentId.ToString()}]{Path.GetExtension(RelatedFile.FullPath)}";
            string DestinationFullPath = Path.Combine(_ThisRootContentFolder, DestinationFileName);

            // Copy and clean up FileUpload entity and uploaded file(s)
            using (ApplicationDbContext Db = GetDbContext())
            using (var Txn = Db.Database.BeginTransaction())  // transactional in case anything throws
            {
                File.Copy(RelatedFile.FullPath, DestinationFullPath, true);
                List<FileUpload> Uploads = Db.FileUpload.Where(f => f.StoragePath == RelatedFile.FullPath).ToList();
                Uploads.ForEach(u => Db.FileUpload.Remove(u));
                File.Delete(RelatedFile.FullPath);
                Db.SaveChanges();
                Txn.Commit();
            }

            JobDetail.Result.ResultingRelatedFiles.Add(new ContentRelatedFile { FilePurpose = RelatedFile.FilePurpose, FullPath = DestinationFullPath });

            if (RelatedFile.FilePurpose.ToLower() == "mastercontent")
            {
                ProcessMasterContentFile(DestinationFullPath, RelatedFile.FilePurpose, RelatedFile.Checksum);
            }
        }

        private async Task<int> CheckRelatedReductionTasks()
        {
            using (ApplicationDbContext Db = GetDbContext())
            {
                List<ContentReductionTask> AllRelatedReductionTasks = await Db.ContentReductionTask.Where(t => t.ContentPublicationRequestId == JobDetail.JobId).ToListAsync();

                if (_CancellationToken.IsCancellationRequested)
                {
                    List<ContentReductionTask> QueuedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Queued).ToList();
                    await CancelReductionTasks(QueuedTasks);
                    _CancellationToken.ThrowIfCancellationRequested();
                }

                List<ContentReductionTask> FailedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Error).ToList(); 
                if (FailedTasks.Any())
                {
                    // Cancel any task still queued
                    List<ContentReductionTask> QueuedTasks = AllRelatedReductionTasks.Where(t => t.ReductionStatus == ReductionStatusEnum.Queued).ToList();
                    await CancelReductionTasks(QueuedTasks);

                    string Msg = $"Publication request terminating due to error in related reduction task(s):{Environment.NewLine}  {string.Join("  " + Environment.NewLine, FailedTasks.Select(t => t.Id.ToString() + " : " + t.ReductionStatusMessage))}";
                    Trace.WriteLine(Msg);

                    throw new ApplicationException(Msg);
                }

                return AllRelatedReductionTasks.Count(t => t.ReductionStatus == ReductionStatusEnum.Queued
                                                        || t.ReductionStatus == ReductionStatusEnum.Reducing);
            }
        }

        /// <summary>
        /// Changes the status of ReductionTask records in the database to Canceled
        /// </summary>
        /// <param name="TasksToCancel"></param>
        /// <returns></returns>
        private async Task<bool> CancelReductionTasks(List<ContentReductionTask> TasksToCancel)
        {
            using (ApplicationDbContext Db = GetDbContext())
            {
                try
                {
                    TasksToCancel.ForEach(t => t.ReductionStatus = ReductionStatusEnum.Canceled);
                    Db.ContentReductionTask.UpdateRange(TasksToCancel);
                    await Db.SaveChangesAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Performs all actions required only in the presence of a master content file in the publication request
        /// Note that a publication request can be made without a content item to update associated files.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="FilePurpose"></param>
        /// <param name="FileChecksum"></param>
        private void ProcessMasterContentFile(string FilePath, string FilePurpose, string FileChecksum)
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = GetDbContext())
            {
                if (!Db.SelectionGroup.Any(sg => sg.RootContentItemId == JobDetail.Request.RootContentId))
                {
                    Db.SelectionGroup.Add(new SelectionGroup
                    {
                        Id = Db.SelectionGroup.Max(sg => sg.Id) + 1,
                        RootContentItemId = JobDetail.Request.RootContentId,
                        GroupName = "Master Content Access",
                        IsMaster = true,
                    });
                    Db.SaveChanges();
                }
            }

            if (JobDetail.Request.DoesReduce)
            {
                using (ApplicationDbContext Db = GetDbContext())
                {
                    bool MasterHierarchyRequested = false;

                    foreach (SelectionGroup SelGrp in Db.SelectionGroup
                                                        .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                        .ToList())
                    {
                        if (SelGrp.IsMaster && MasterHierarchyRequested)
                        {
                            // Only handle IsMaster SelectionGroups once
                            // TODO During go-live the master file will need to be associated with all IsMaster groups
                            continue;
                        }

                        Guid TaskId = Guid.NewGuid();

                        string QvSourceDocumentsPath = Configuration.ApplicationConfiguration.GetSection("Storage")["QvSourceDocumentsPath"];
                        string TaskFolder = Path.Combine(QvSourceDocumentsPath, TaskId.ToString());
                        Directory.CreateDirectory(TaskFolder);

                        string DestinationFileName = $"{FilePurpose}.Pub[{JobDetail.JobId.ToString()}].Content[{JobDetail.Request.RootContentId.ToString()}]{Path.GetExtension(FilePath)}";
                        string CopyDestination = Path.Combine(TaskFolder, DestinationFileName);
                        File.Copy(FilePath, CopyDestination, true);

                        ContentReductionTask NewTask = new ContentReductionTask
                        {
                            Id = TaskId,
                            ApplicationUserId = JobDetail.Request.ApplicationUserId,
                            ContentPublicationRequestId = JobDetail.JobId,
                            CreateDateTimeUtc = DateTime.UtcNow,  // TODO later: Figure out how to avoid delay in starting the reduction task. 
                            MasterFilePath = CopyDestination,
                            MasterContentChecksum = FileChecksum,
                            ReductionStatus = ReductionStatusEnum.Queued,
                            SelectionGroupId = SelGrp.Id,
                        };

                        if (SelGrp.IsMaster)
                        {
                            NewTask.TaskAction = TaskActionEnum.HierarchyOnly;

                            MasterHierarchyRequested = true;
                        }
                        else
                        {
                            var SelectionHierarchy = ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(Db, SelGrp.Id);

                            NewTask.SelectionCriteria = SelectionHierarchy.SerializeJson();
                            NewTask.TaskAction = TaskActionEnum.HierarchyAndReduction;
                        }

                        Db.ContentReductionTask.Add(NewTask);
                        Db.SaveChanges();
                    }
                }

            }
            else
            {
                // nothing?
            }
        }
    }
}
