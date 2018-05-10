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
using AuditLogLib;
using MapDbContextLib.Context;
using Moq;

namespace ContentPublishingLib.JobRunners
{
    public class MapDbPublishRunner : RunnerBase
    {
        private DbContextOptions<ApplicationDbContext> ContextOptions = null;
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

        public async Task<PublishJobDetail> Execute(CancellationToken cancellationToken)
        {
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            _CancellationToken = cancellationToken;

            MethodBase Method = MethodBase.GetCurrentMethod();
            object DetailObj;
            AuditEvent Event;

            try
            {
                string StorageBasePath = Configuration.ApplicationConfiguration.GetSection("Storage")["LiveContentRootPath"];
                if (!Directory.Exists(StorageBasePath))
                {
                    throw new ApplicationException($"Configured LiveContentRootPath folder {StorageBasePath} does not exist");
                }

                string RootContentFolder = Path.Combine(StorageBasePath, JobDetail.Request.RootContentId.ToString());
                DirectoryInfo ContentDirectoryInfo = Directory.CreateDirectory(RootContentFolder);

                // Handle each file related to this PublicationRequest
                foreach (PublishJobDetail.ContentRelatedFile RelatedFile in JobDetail.Request.RelatedFiles)
                {
                    if (!File.Exists(RelatedFile.FullPath))
                    {
                        throw new ApplicationException($"While publishing request {JobDetail.JobId.ToString()}, uploaded file not found at path [{RelatedFile.FullPath}].");
                    }

                    switch (RelatedFile.FilePurpose.ToLower())
                    {
                        case "mastercontent":
                            ProcessMasterContentFile(RelatedFile);
                            break;

                        default:
                            string DestinationFileName = $"{RelatedFile.FilePurpose}.Pub[{JobDetail.JobId.ToString()}].Content[{JobDetail.Request.RootContentId.ToString()}]{Path.GetExtension(RelatedFile.FullPath).Replace("..", ".")}";
                            string DestinationFullPath = Path.Combine(RootContentFolder, DestinationFileName);

                            File.Copy(RelatedFile.FullPath, DestinationFullPath, true);

                            JobDetail.Result.RelatedFiles.Add(new PublishJobDetail.ContentRelatedFile { FilePurpose = RelatedFile.FilePurpose, FullPath = DestinationFullPath });

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                JobDetail.Status = PublishJobDetail.JobStatusEnum.Error;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }

            return JobDetail;
        }

        /// <summary>
        /// Performs all actions required only in the presence of a master content file in the publication request
        /// Note that a publication request can be made without a content item to update associated files.
        /// </summary>
        /// <param name="MasterFile"></param>
        private void ProcessMasterContentFile(PublishJobDetail.ContentRelatedFile MasterFile)
        {
            // If there is no SelectionGroup for this content item, create a new SelectionGroup with IsMaster = true
            using (ApplicationDbContext Db = MockContext != null
                                           ? MockContext.Object
                                           : new ApplicationDbContext(ContextOptions))
            {
                if (!Db.SelectionGroup.Any(sg => sg.RootContentItemId == JobDetail.Request.RootContentId))
                {
                    Db.SelectionGroup.Add(new SelectionGroup
                    {
                        RootContentItemId = JobDetail.Request.RootContentId,
                        GroupName = "Master Content Access",
                        IsMaster = true,
                    });
                    Db.SaveChanges();
                }
            }

            if (JobDetail.Request.DoesReduce)
            {
                using (ApplicationDbContext Db = MockContext != null
                                               ? MockContext.Object
                                               : new ApplicationDbContext(ContextOptions))
                {
                    bool MasterHierarchyRequested = false;

                    foreach (SelectionGroup SelGrp in Db.SelectionGroup
                                                        .Where(g => g.RootContentItemId == JobDetail.Request.RootContentId)
                                                        .ToList())
                    {
                        if (SelGrp.IsMaster && MasterHierarchyRequested)
                        {
                            // Only handle IsMaster SelectionGroups once
                            continue;
                        }

                        ContentReductionTask NewTask = new ContentReductionTask
                        {
                            ApplicationUserId = JobDetail.Request.ApplicationUserId,
                            ContentPublicationRequestId = JobDetail.JobId,
                            CreateDateTime = DateTime.UtcNow,  // TODO later: Figure out how to avoid delay in starting the reduction task. 
                            MasterContentChecksum = MasterFile.Checksum,
                            MasterFilePath = MasterFile.FullPath,
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
                            var x = Db.HierarchyFieldValue.Where(v => SelGrp.SelectedHierarchyFieldValueList.Contains(v.Id));
                            NewTask.SelectionCriteria = "#";
                            NewTask.TaskAction = TaskActionEnum.HierarchyAndReduction;
                        }

                        Db.ContentReductionTask.Add(NewTask);
                        Db.SaveChanges();
                    }
                }

            }
            else
            {
                // Do I need to remove any possible files for this publish request ID?  (IDK maybe there will never be any). 

                // Copy all related files to RootContentItem folder
                foreach (PublishJobDetail.ContentRelatedFile RelatedFile in JobDetail.Request.RelatedFiles)
                {
                }

                JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;
            }
        }
    }
}
