/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ReductionSummary
    {
        public UserInfoViewModel User { get; set; }
        public ReductionStatusEnum StatusEnum { get; set; }
        public string StatusName { get; set; }
        public string StatusMessage { get; set; }
        public Guid SelectionGroupId { get; set; }
        public Guid? RootContentItemId { get; set; }
        public int QueuedDurationMs { get; set; }
        public int QueuePosition { get; set; } = -1;

        public static explicit operator ReductionSummary(ContentReductionTask contentReductionTask)
        {
            if (contentReductionTask == null || !contentReductionTask.SelectionGroupId.HasValue)
            {
                return null;
            }
            return new ReductionSummary
            {
                User = ((UserInfoViewModel)contentReductionTask.ApplicationUser),
                StatusEnum = contentReductionTask.ReductionStatus,
                StatusName = ContentReductionTask.ReductionStatusDisplayNames[contentReductionTask.ReductionStatus],
                SelectionGroupId = contentReductionTask.SelectionGroupId.Value,
                RootContentItemId = contentReductionTask.ContentPublicationRequest?.RootContentItemId,
                QueuedDurationMs = (int)(contentReductionTask.ReductionStatus.IsActive()
                    ? DateTime.UtcNow - contentReductionTask.CreateDateTimeUtc
                    : TimeSpan.Zero).TotalMilliseconds,
            };
        }
    }

    public static class ReductionSummaryExtensions
    {
        public static ReductionSummary ToSummaryWithQueueInformation(this ContentReductionTask reductionTask, ApplicationDbContext dbContext)
        {
            var reductionSummary = (ReductionSummary)reductionTask;

            if (reductionTask?.ReductionStatus.IsCancelable() ?? false)
            {
                var precedingReductionTaskCount = dbContext.ContentReductionTask
                    .Where(r => r.CreateDateTimeUtc < reductionTask.CreateDateTimeUtc)
                    .Where(r => r.ReductionStatus.IsCancelable())
                    .Count();
                reductionSummary.QueuePosition = precedingReductionTaskCount;
            }

            if (!string.IsNullOrWhiteSpace(reductionTask.OutcomeMetadata))
            {
                // Assemble the list of messages for all failed reductions
                string message;
                switch (reductionTask.OutcomeMetadataObj.OutcomeReason)
                {
                    case MapDbReductionTaskOutcomeReason.SelectionForInvalidFieldName:
                        message = "A value in an invalid field was selected.";
                        break;
                    case MapDbReductionTaskOutcomeReason.NoReducedFileCreated:
                        message = "The selected values do not match any data.";
                        break;
                    default:
                        message = "Unexpected error. Please retry the selection update and "
                            + "contact support if the problem persists.";
                        break;
                }

                reductionSummary.StatusMessage = message;
            }

            return reductionSummary;
        }
    }
}
