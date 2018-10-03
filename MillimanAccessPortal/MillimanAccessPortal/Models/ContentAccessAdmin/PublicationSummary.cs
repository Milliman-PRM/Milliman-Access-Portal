/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class PublicationSummary
    {
        public UserInfoViewModel User { get; set; }
        public PublicationStatus StatusEnum { get; set; }
        public string StatusName { get => ContentPublicationRequest.PublicationStatusString[StatusEnum]; }
        public string StatusMessage { get; set; } = string.Empty;
        public Guid RootContentItemId { get; set; }
        public TimeSpan QueuedDuration { get; set; }
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
                StatusMessage = contentPublicationRequest.StatusMessage,
                QueuedDuration = contentPublicationRequest.RequestStatus.IsActive()
                    ? DateTime.UtcNow - contentPublicationRequest.CreateDateTimeUtc
                    : TimeSpan.Zero,
            };
        }
    }

    public static class PublicationSummaryExtensions
    {
        public static PublicationSummary ToSummaryWithQueueInformation(this ContentPublicationRequest publicationRequest, ApplicationDbContext dbContext)
        {
            var publicationSummary = (PublicationSummary)publicationRequest;

            if (publicationRequest.RequestStatus.IsCancelable())
            {
                var precedingPublicationRequestCount = dbContext.ContentPublicationRequest
                    .Where(r => r.CreateDateTimeUtc < publicationRequest.CreateDateTimeUtc)
                    .Where(r => r.RequestStatus.IsCancelable())
                    .Count();
                publicationSummary.QueuePosition = precedingPublicationRequestCount;
            }
            else if (publicationRequest.RequestStatus.IsActive())
            {
                var relatedReductionTaskCount = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                    .Count();
                var completedReductionTaskCount = dbContext.ContentReductionTask
                    .Where(t => t.ContentPublicationRequestId == publicationRequest.Id)
                    .Where(t => !t.ReductionStatus.IsCancelable())
                    .Count();
                publicationSummary.QueuePosition = completedReductionTaskCount;
                publicationSummary.QueueTotal = relatedReductionTaskCount;
            }

            return publicationSummary;
        }
    }
}
