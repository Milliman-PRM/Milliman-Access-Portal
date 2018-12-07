/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Declares a class representing the outcome of reduction task processing
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MapDbContextLib.Models
{
    public enum MapDbReductionTaskOutcomeReason
    {
        Default = 0,

        Success = 10,
        Canceled = 11,
        BadRequest = 12,

        UnspecifiedError = 100,
        NoSelectedFieldValues = 101,
        NoSelectedFieldValueMatchInNewContent = 102,
    }

    /// <summary>
    /// A type representing reduction task processing outcome. Could be used as a base class to add extended outcome specific details. 
    /// </summary>
    public class ReductionTaskOutcomeMetadata
    {
        public Guid ReductionTaskId { get; set; }
        public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;
        public MapDbReductionTaskOutcomeReason OutcomeReason { get; set; } = MapDbReductionTaskOutcomeReason.Default;
    }
}
