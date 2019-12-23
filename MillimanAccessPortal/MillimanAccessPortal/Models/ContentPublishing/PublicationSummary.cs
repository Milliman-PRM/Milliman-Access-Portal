/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublicationSummary
    {
        public UserInfoViewModel User { get; set; }
        public PublicationStatus StatusEnum { get; set; }
        public string StatusName { get => ContentPublicationRequest.PublicationStatusString[StatusEnum]; }
        public string StatusMessage { get; set; } = string.Empty;
        public Guid RootContentItemId { get; set; }
        public int QueuedDurationMs { get; set; }
        public int QueuePosition { get; set; } = -1;
        public int QueueTotal { get; set; } = -1;

        public static explicit operator PublicationSummary(ContentPublicationRequest contentPublicationRequest)
        {
            if (contentPublicationRequest == null)
            {
                return null;
            }
            return new PublicationSummary
            {
                User = (UserInfoViewModel) contentPublicationRequest.ApplicationUser,
                StatusEnum = contentPublicationRequest.RequestStatus,
                RootContentItemId = contentPublicationRequest.RootContentItemId,
                QueuedDurationMs = (int)(contentPublicationRequest.RequestStatus.IsActive()
                    ? DateTime.UtcNow - contentPublicationRequest.CreateDateTimeUtc
                    : TimeSpan.Zero).TotalMilliseconds,
            };
        }
    }

    public static class PublicationSummaryExtensions
    {
        public static PublicationSummary ToSummaryWithQueueInformation(this ContentPublicationRequest publicationRequest, ApplicationDbContext dbContext)
        {
            var publicationSummary = (PublicationSummary)publicationRequest;

            if (PublicationStatusExtensions.QueueWaitableStatusList.Contains(publicationRequest.RequestStatus))
            {
                var precedingPublicationRequestCount = dbContext.ContentPublicationRequest
                    .Where(r => r.CreateDateTimeUtc < publicationRequest.CreateDateTimeUtc)
                    .Where(r => PublicationStatusExtensions.CancelablePublicationStatusList.Contains(r.RequestStatus))
                    .Count();
                publicationSummary.QueuePosition = precedingPublicationRequestCount;
            }
            else if (publicationRequest?.RequestStatus.IsActive() ?? false)
            {
                var relatedReductionTaskCount = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                    .Where(t => t.SelectionGroup != null)
                    .Count();
                var completedReductionTaskCount = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                    .Where(t => t.SelectionGroup != null)
                    .Where(t => t.ReductionStatus != ReductionStatusEnum.Queued)
                    .Where(t => t.ReductionStatus != ReductionStatusEnum.Reducing)
                    .Count();
                publicationSummary.QueuePosition = completedReductionTaskCount;
                publicationSummary.QueueTotal = relatedReductionTaskCount;
            }

            // Assemble the list of messages for all failed reductions
            if (publicationRequest != null)
            {
                var messages = new List<string> { };
                foreach (var taskOutcome in publicationRequest.OutcomeMetadataObj.ReductionTaskFailOutcomeList)
                {
                    messages.Add(taskOutcome.OutcomeReason.GetDisplayDescriptionString(true));
                }

                // don't overwhelm the user with a giant error message
                publicationSummary.StatusMessage = messages.FirstOrDefault();
            }

            return publicationSummary;
        }
    }
}
