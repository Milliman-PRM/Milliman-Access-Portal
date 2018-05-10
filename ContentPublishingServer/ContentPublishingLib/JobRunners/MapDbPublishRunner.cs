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
                if (JobDetail.Request.DoesReduce)
                {
                }
                else
                {
                    string StorageBasePath = Configuration.ApplicationConfiguration.GetSection("Storage")["LiveContentRootPath"];

                    string RootContentFolder = Path.Combine(StorageBasePath, JobDetail.Request.RootContentIdString);
                    DirectoryInfo ContentDirectoryInfo = Directory.CreateDirectory(RootContentFolder);

                    foreach (PublishJobDetail.ContentRelatedFile F in JobDetail.Request.RelatedFiles)
                    {
                        if (!File.Exists(F.FullPath))
                        {
                            throw new ApplicationException($"While publishing request {JobDetail.JobId}, uploaded file not found at path [{F.FullPath}].");
                        }
                        string NewFileName = $"{F.FilePurpose}.PubRequest[{JobDetail.Request.RootContentIdString.ToString()}]{Path.GetExtension(F.FullPath).Replace("..", ".")}";
                        string DestinationFile = Path.Combine(RootContentFolder, NewFileName);

                        File.Copy(F.FullPath, DestinationFile, true);

                        JobDetail.Result.RelatedFiles.Add(new PublishJobDetail.ContentRelatedFile { FilePurpose = F.FilePurpose, FullPath = DestinationFile });

                        JobDetail.Status = PublishJobDetail.JobStatusEnum.Success;
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
    }
}
