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

            if (JobDetail.Request.DoesReduce)
            {
            }
            else
            {
                string StorageBasePath = Configuration.ApplicationConfiguration.GetSection("Storage")["LiveContentRootPath"];

                string RootContentFolder = Path.Combine(StorageBasePath, JobDetail.Request.RootContentIdString);
                DirectoryInfo ContentDirectoryInfo = Directory.CreateDirectory(RootContentFolder);

                using (ApplicationDbContext Db = MockContext != null
                                                 ? MockContext.Object
                                                 : new ApplicationDbContext(ContextOptions))
                {
                    foreach (PublishJobDetail.ContentRelatedFile F in JobDetail.Request.RelatedFiles)
                    {
                        FileUpload UploadedFile = Db.FileUpload.Find(F.FileUploadId);
                        if (UploadedFile == null || !File.Exists(UploadedFile.StoragePath))
                        {
                            throw new ApplicationException($"While publishing request {JobDetail.JobId}");
                        }
                        string NewFileName = $"{F.FilePurpose}.PubRequest[{JobDetail.Request.RootContentIdString.ToString()}]{Path.GetExtension(UploadedFile.StoragePath)}";
                        string DestinationFile = Path.Combine(RootContentFolder, NewFileName);

                        File.Copy(UploadedFile.StoragePath, DestinationFile);
                    }
                }
            }

            JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;

            _CancellationToken.ThrowIfCancellationRequested();

            return JobDetail;
        }
    }
}
