/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A page level view model to convey static information to the front end
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ClientAccessReviewGlobalDataModel
    {
        public int ClientReviewEarlyWarningDays { get; set; }
    }
}
