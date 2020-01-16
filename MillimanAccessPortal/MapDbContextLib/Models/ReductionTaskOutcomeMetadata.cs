/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Declares a class representing the outcome of reduction task processing
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MapDbContextLib.Models
{
    public enum MapDbReductionTaskOutcomeReason
    {
        [Display(Description = "")]
        Default = 0,

        [Display(Description = "The reduction task completed successfully")]
        Success = 10,

        [Display(Description = "The unreduced content file has been successfully assigned to this selection group")]
        MasterHierarchyAssigned = 11,

        [Display(Description = "The reduction task was canceled")]
        Canceled = 20,

        [Display(Description = "Bad request, an internal reduction processing error occurred")]
        BadRequest = 21,

        [Display(Description = "An unspecified error has occurred. Please retry the request and contact support if the problem persists")]
        UnspecifiedError = 100,

        [Display(Description = "No field values were selected for this reduction")]
        NoSelectedFieldValues = 101,

        [Display(Description = "None of the selected values for this selection group exist in the new content file")]
        NoSelectedFieldValueExistsInNewContent = 102,

        [Display(Description = "A value was selected for an invalid field")]
        SelectionForInvalidFieldName = 103,

        [Display(Description = "A reduced file was not created")]
        NoReducedFileCreated = 104,

        [Display(Description = "The reduction task timed out. Please retry the request and contact support if the problem persists")]
        ReductionTimeout = 105,

        [Display(Description = "The content hierarchy failed to export from the content file or could not be interpreted")]
        HierarchyExtractionFailed = 106,
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

        public DateTime? ProcessingStartedUtc { get; set; } = null;

        [JsonConverter(typeof(StringEnumConverter))]
        public MapDbReductionTaskOutcomeReason OutcomeReason { get; set; } = MapDbReductionTaskOutcomeReason.Default;

        public string SelectionGroupName { get; set; } = default;

        public string UserMessage { get; set; } = default;

        public string SupportMessage { get; set; } = default;
    }
}
