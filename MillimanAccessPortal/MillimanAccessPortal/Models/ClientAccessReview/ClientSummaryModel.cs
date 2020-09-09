/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ClientSummaryModel
    {
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public DateTime ReviewDueDate { get; set; }
        public DateTime LastReviewDate { get; set; }
        public string LastReviewedBy { get; set; }
        public string PrimaryContactName { get; set; }
        public string PrimaryContactEmail { get; set; }
        public string AssignedProfitCenter {get;set;}
        public List<ClientActorModel> ClientAdmins { get; set; } = new List<ClientActorModel>();
        public List<ClientActorModel> ProfitCenterAdmins { get; set; } = new List<ClientActorModel>();
    }

    public class ClientActorModel
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }
}
