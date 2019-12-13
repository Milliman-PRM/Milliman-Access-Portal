/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    /// <summary>
    /// A simplified representation of a ContentReductionTask.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicReduction
    {
        public Guid Id { get; set; }
        public Guid? ContentPublicationRequestId { get; set; }
        public BasicUser ApplicationUser { get; set; }
        public Guid? SelectionGroupId { get; set; }
        public List<Guid> SelectedValues { get; set; }
        public DateTime CreateDateTimeUtc { get; set; }
        public ReductionStatusEnum TaskStatus { get; set; }
        public string TaskStatusMessage { get; set; }

        public static explicit operator BasicReduction(ContentReductionTask reduction)
        {
            if (reduction == null)
            {
                return null;
            }

            string message = null;
            if (!string.IsNullOrWhiteSpace(reduction.OutcomeMetadata))
            {
                switch (reduction.OutcomeMetadataObj.OutcomeReason)
                {
                    case MapDbReductionTaskOutcomeReason.SelectionForInvalidFieldName:
                        message = "A value in an invalid field was selected.";
                        break;
                    case MapDbReductionTaskOutcomeReason.NoReducedFileCreated:
                        message = "The selected values did not match any data.";
                        break;
                    default:
                        message = "Unexpected error. Please retry the selection update and "
                            + "contact support if the problem persists.";
                        break;
                }
            }

            return new BasicReduction
            {
                Id = reduction.Id,
                ContentPublicationRequestId = reduction.ContentPublicationRequestId,
                ApplicationUser = (BasicUser)reduction.ApplicationUser,
                SelectionGroupId = reduction.SelectionGroupId,
                SelectedValues = reduction.SelectionCriteriaObj?.GetSelectedValueIds(),
                CreateDateTimeUtc = reduction.CreateDateTimeUtc,
                TaskStatus = reduction.ReductionStatus,
                TaskStatusMessage = message,
            };
        }
    }
}
