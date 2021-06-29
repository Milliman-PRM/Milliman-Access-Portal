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

        public ClientReviewDeadlineStatus DeadlineStatus { get; set; } = ClientReviewDeadlineStatus.Unspecified;

        public ClientReviewModel(Client c, int reviewPeriodDays, int earlyWarningPeriodDays, string userTimeZone) : base(c)
        {
            ReviewDueDateTime = GlobalFunctions.UtcToLocalString(c.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(reviewPeriodDays), userTimeZone);
            DeadlineStatus = c.LastAccessReview.LastReviewDateTimeUtc switch
            {
                DateTime dt when dt < DateTime.UtcNow - TimeSpan.FromDays(reviewPeriodDays) => ClientReviewDeadlineStatus.Expired,
                DateTime dt when dt < DateTime.UtcNow - TimeSpan.FromDays(reviewPeriodDays) + TimeSpan.FromDays(earlyWarningPeriodDays) => ClientReviewDeadlineStatus.EarlyWarning,
                _ => ClientReviewDeadlineStatus.Current,
            };
        }
    }
}
