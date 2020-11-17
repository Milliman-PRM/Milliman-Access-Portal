/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Client model supporting the client review View
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.EntityModels.ClientModels
{
    public class ClientReviewModel : BasicClient
    {
        public DateTime ReviewDueDateTimeUtc { get; set; }

        public ClientReviewModel(Client c, int reviewPeriodDays) : base(c)
        {
            ReviewDueDateTimeUtc = c.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(reviewPeriodDays);
        }
    }
}
