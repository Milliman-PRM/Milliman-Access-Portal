/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Represents reduction job outputs and status, agnostic to the types used by the application that originated the queued task
 * DEVELOPER NOTES: This gets converted to queue specific 
 */

using System;

namespace ContentReductionLib.ReductionRunners
{
    internal enum ReductionJobStatusEnum
    {
        Unspecified,
        Canceled,
        Success,
        Error,
    }

    internal class ReductionJobResult
    {
        internal Guid TaskId { get; set; } = Guid.Empty;

        internal ReductionJobStatusEnum Status { get; set; } = ReductionJobStatusEnum.Unspecified;

        internal string ReducedContentFilePath { get; set; } = string.Empty;

        internal ExtractedHierarchy ExtractedHierarchy { get; set; } = null;

        internal string UserMessage { get; set; } = string.Empty;
    }
}
