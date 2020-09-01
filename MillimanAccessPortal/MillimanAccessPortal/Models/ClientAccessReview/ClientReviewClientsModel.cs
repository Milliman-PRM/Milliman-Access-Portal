/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model conveying relevant clients with supporting card properties for the ClientAccessReview view
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.EntityModels.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ClientReviewClientsModel
    {
        /// <summary>
        /// Clients that can be reviewed
        /// </summary>
        public Dictionary<Guid, ClientReviewModel> Clients { get; set; }

        /// <summary>
        /// Clients that have children the user can review but cannot be reviewed themselves
        /// </summary>
        public Dictionary<Guid, ClientReviewModel> ParentClients { get; set; }
    }

    public class ClientReviewGlobalDataModel
    {
        public int ClientReviewEarlyWarningDays { get; set; }

        public int ClientReviewGracePeriodDays { get; set; }
    }
}
