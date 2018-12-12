/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;
using MapDbContextLib.Models;

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

        public enum JobErrorReason
        {
            Unspecified,
            ReductionTaskErrors,
        }

        public PublishJobRequest Request;
        public PublishJobResult Result;
        public Guid JobId { get; set; } = Guid.Empty;
        public JobStatusEnum Status { get; set; } = JobStatusEnum.Unspecified;
        public JobErrorReason StatusReason { get; set; } = JobErrorReason.Unspecified;

        // cast operator to convert a MAP ContentReductionTask to this type
        public static PublishJobDetail New(ContentPublicationRequest DbTask, ApplicationDbContext Db)
        {
            return new PublishJobDetail
            {
                JobId = DbTask.Id,
                Request = new PublishJobRequest
                {
                    DoesReduce = DbTask.RootContentItem.DoesReduce,
                    MasterContentFile = DbTask.ReductionRelatedFilesObj.Select(rrf => rrf.MasterContentFile).SingleOrDefault(),
                    RootContentId = DbTask.RootContentItemId,
                    ApplicationUserId = DbTask.ApplicationUserId,
                    CreateDateTimeUtc = DbTask.CreateDateTimeUtc,
                },
                Result = new PublishJobResult(),
            };

        }

        public class PublishJobResult
        {
            public string StatusMessage { get; set; } = string.Empty;
            public List<ContentRelatedFile> ResultingRelatedFiles { get; set; } = new List<ContentRelatedFile>();
            public List<ReductionTaskOutcomeMetadata> ReductionTaskFailList { get; set; } = new List<ReductionTaskOutcomeMetadata>();
            public List<ReductionTaskOutcomeMetadata> ReductionTaskSuccessList { get; set; } = new List<ReductionTaskOutcomeMetadata>();
        }

        public class PublishJobRequest
        {
            public bool DoesReduce { get; set; }
            public ContentRelatedFile MasterContentFile { get; set; }
            public Guid RootContentId { get; set; }
            public Guid ApplicationUserId { get; set; }
            public DateTime CreateDateTimeUtc { get; set; }
        }

    }
}
