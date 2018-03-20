/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Reduction job status that is agnostic to the original queue mechanism status tracking concepts
 * DEVELOPER NOTES: Intended as part of support for other sources of reduction jobs
 */

namespace ContentReductionLib.ReductionRunners
{
    public enum ReductionJobResultEnum
    {
        Success,
        Error,
    }
}
