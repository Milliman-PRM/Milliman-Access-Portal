/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Client model supporting the client review View
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using MapDbContextLib.Context;
using System;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.EntityModels.ClientModels
{
    public enum ClientReviewDeadlineStatus
    {
        [Display(Name = "Unspecified")]
        Unspecified = 0,

        [Display(Name = "Current")]
        Current,

        [Display(Name = "Early Warning")]
        EarlyWarning,

        [Display(Name = "Expired")]
        Expired
    }

    public class ClientReviewModel : BasicClient
    {
        public string ReviewDueDateTime { get; set; }

        public string SortableDueDateTime { get; set; }

        public ClientReviewDeadlineStatus DeadlineStatus { get; set; } = ClientReviewDeadlineStatus.Unspecified;

        public ClientReviewModel(Client c, int reviewPeriodDays, int earlyWarningPeriodDays, int ClientReviewNotificationHourOfDayUtc, string userTimeZone) : base(c)
        {
            DateTime nextReviewDateTimeUtc = c.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(reviewPeriodDays);
            ReviewDueDateTime = GlobalFunctions.UtcToLocalString(nextReviewDateTimeUtc, userTimeZone);
            SortableDueDateTime = GlobalFunctions.UtcToSortableDateString(nextReviewDateTimeUtc, userTimeZone);
            DeadlineStatus = c.LastAccessReview.LastReviewDateTimeUtc switch
            {
                DateTime dt when dt < DateTime.UtcNow - TimeSpan.FromDays(reviewPeriodDays) => ClientReviewDeadlineStatus.Expired,
                DateTime dt when dt < (DateTime.UtcNow - TimeSpan.FromDays(reviewPeriodDays) + TimeSpan.FromDays(earlyWarningPeriodDays)).Date + TimeSpan.FromHours(ClientReviewNotificationHourOfDayUtc) => ClientReviewDeadlineStatus.EarlyWarning,
                _ => ClientReviewDeadlineStatus.Current,
            };
        }
    }
}
