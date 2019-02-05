using MapDbContextLib.Context;
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class BasicReduction
    {
        public Guid Id { get; set; }
        public Guid? ContentPublicationRequestId { get; set; }
        public Guid ApplicationUserId { get; set; }
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
                ApplicationUserId = reduction.ApplicationUserId,
                SelectionGroupId = reduction.SelectionGroupId,
                SelectedValues = reduction.SelectionCriteriaObj?.GetSelectedValueIds(),
                CreateDateTimeUtc = reduction.CreateDateTimeUtc,
                TaskStatus = reduction.ReductionStatus,
                TaskStatusMessage = message,
            };
        }
    }
}
