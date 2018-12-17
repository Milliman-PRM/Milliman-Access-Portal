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
        Canceled = 11,
        BadRequest = 12,

        UnspecifiedError = 100,
        NoSelectedFieldValues = 101,
        NoSelectedFieldValueMatchInNewContent = 102,
        SelectionForInvalidFieldName = 103,
    }
    public static class MapDbReductionTaskOutcomeReasonExtensions
    {
        public static bool PreventsPublication(this MapDbReductionTaskOutcomeReason reason)
        {
            // MAP knows how to handle these errors so don't prevent a publication if they happen
            var okayReasons = new List<MapDbReductionTaskOutcomeReason>
            {
                MapDbReductionTaskOutcomeReason.NoSelectedFieldValues,
                MapDbReductionTaskOutcomeReason.NoSelectedFieldValueMatchInNewContent,
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
        public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;
        public MapDbReductionTaskOutcomeReason OutcomeReason { get; set; } = MapDbReductionTaskOutcomeReason.Default;
    }
}
