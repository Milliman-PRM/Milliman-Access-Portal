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
        MasterHierarchyAssigned = 11,

        Canceled = 20,
        BadRequest = 21,

        UnspecifiedError = 100,
        NoSelectedFieldValues = 101,
        NoSelectedFieldValueExistsInNewContent = 102,
        SelectionForInvalidFieldName = 103,
        NoReducedFileCreated = 104,
    }

    /// <summary>
    /// A type representing reduction task processing outcome. Could be used as a base class to add extended outcome specific details. 
    /// </summary>
    public class ReductionTaskOutcomeMetadata
    {
        public Guid ReductionTaskId { get; set; }
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        public MapDbReductionTaskOutcomeReason OutcomeReason { get; set; } = MapDbReductionTaskOutcomeReason.Default;
    }
}
