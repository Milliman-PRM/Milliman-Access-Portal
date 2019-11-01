/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Declares a class representing the outcome of reduction task processing
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;

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
        ReductionTimeout = 105,
    }
    public static class MapDbReductionTaskOutcomeReasonExtensions
    {
        /// <summary>
        /// Whether a reduction outcome reason indicates its publication should be aborted
        /// </summary>
        /// <remarks>Applies to all outcome reasons regardless of reduction task status</remarks>
        public static bool PreventsPublication(this MapDbReductionTaskOutcomeReason reason)
        {
            // MAP knows how to handle these outcomes so don't prevent a publication if they happen
            // Whitelist outcome reasons that allow publication. For outcome reasons associated with error status,
            // only whitelist if it results from expected system usage AND it can be handled gracefully.
            var okayReasons = new List<MapDbReductionTaskOutcomeReason>
            {
                MapDbReductionTaskOutcomeReason.Success,
                MapDbReductionTaskOutcomeReason.MasterHierarchyAssigned,
                MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent,
                MapDbReductionTaskOutcomeReason.NoReducedFileCreated,
            };

            return !okayReasons.Contains(reason);
        }
    }

    /// <summary>
    /// A type representing reduction task processing outcome. Could be used as a base class to add extended outcome specific details. 
    /// </summary>
    public class ReductionTaskOutcomeMetadata
    {
        public Guid ReductionTaskId { get; set; }
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        public DateTime? ProcessingStarted { get; set; } = null;
        public MapDbReductionTaskOutcomeReason OutcomeReason { get; set; } = MapDbReductionTaskOutcomeReason.Default;
    }
}
