using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;

namespace ContentPublishingLib.JobRunners
{
    public class PublishJobDetail
    {
        public enum JobStatusEnum
        {
            Unspecified,
            Canceled,
            Processing,
            Success,
            Error,
        }

        public PublishJobRequest Request;
        public PublishJobResult Result;
        public long JobId { get; set; } = -1;
        public JobStatusEnum Status { get; set; } = JobStatusEnum.Unspecified;

        // cast operator to convert a MAP ContentReductionTask to this type
        public static PublishJobDetail New(ContentPublicationRequest DbTask, ApplicationDbContext Db)
        {
            return new PublishJobDetail
            {
                JobId = DbTask.Id,
                Request = new PublishJobRequest
                {
                    DoesReduce = DbTask.RootContentItem.DoesReduce,
                    RelatedFiles = DbTask.PublishRequest.RelatedFiles.Select(rf =>
                        {
                            var UploadRecord = Db.FileUpload.Find(rf.FileUploadId);
                            return new ContentRelatedFile
                            {
                                FilePurpose = rf.FilePurpose,
                                FullPath = UploadRecord.StoragePath,
                                Checksum = UploadRecord.Checksum,
                            };
                        }
                    ).ToList(),
                    RootContentId = DbTask.RootContentItemId,
                    ApplicationUserId = DbTask.ApplicationUserId,
                },
                Result = new PublishJobResult(),
            };

        }

        public class PublishJobResult
        {
            public string StatusMessage { get; set; } = string.Empty;
            public List<ContentRelatedFile> RelatedFiles { get; set; } = new List<ContentRelatedFile>();
        }

        public class PublishJobRequest
        {
            public bool DoesReduce { get; set; }
            public List<ContentRelatedFile> RelatedFiles { get; set; }
            public long RootContentId { get; set; }
            public long ApplicationUserId { get; set; }
        }

        public class ContentRelatedFile
        {
            public string FilePurpose { get; set; }
            public string FullPath { get; set; }
            public string Checksum { get; set; }
        }

    }
}
