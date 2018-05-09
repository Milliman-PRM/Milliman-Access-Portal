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
        public static explicit operator PublishJobDetail(ContentPublicationRequest DbTask)
        {
            return new PublishJobDetail
            {
                JobId = DbTask.Id,
                Request = new PublishJobRequest
                {
                    DoesReduce = DbTask.RootContentItem.DoesReduce,
                    RelatedFiles = DbTask.PublishRequest.RelatedFiles.Select(rf =>
                        new ContentRelatedFile
                        {
                            FilePurpose = rf.FilePurpose,
                            FileUploadId = rf.FileUploadId,
                        }
                    ).ToList(),
                    RootContentIdString = DbTask.RootContentItem.Id.ToString(),
                },
                Result = new PublishJobResult(),
            };

        }

        public class PublishJobResult
        {
        }

        public class PublishJobRequest
        {
            public bool DoesReduce { get; set; }
            public List<ContentRelatedFile> RelatedFiles { get; set; }
            public string RootContentIdString { get; set; }
        }

        public class ContentRelatedFile
        {
            public string FilePurpose { get; set; }
            public Guid FileUploadId { get; set; }

            public static explicit operator ContentRelatedFile(MapDbContextLib.Models.ContentRelatedFile Arg)
            {
                return new ContentRelatedFile
                {
                    FilePurpose = Arg.FilePurpose,
                    FileUploadId = Arg.FileUploadId,
                };
            }
        }

    }
}
