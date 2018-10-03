/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
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
                QueuedDuration = DateTime.UtcNow - contentPublicationRequest.CreateDateTimeUtc,
            };
        }
    }
}
