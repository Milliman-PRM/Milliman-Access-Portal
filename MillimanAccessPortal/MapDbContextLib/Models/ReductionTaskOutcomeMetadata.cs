/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public enum MapDbReductionTaskOutcomeReason
    {
        Default = 0,
        Success = 1,

        NoSelectedFieldValueMatchesNewContent = 100,
    }

    public class ReductionTaskOutcomeMetadata
    {
        public MapDbReductionTaskOutcomeReason OutcomeSummary { get; set; } = MapDbReductionTaskOutcomeReason.Default;
    }
}
