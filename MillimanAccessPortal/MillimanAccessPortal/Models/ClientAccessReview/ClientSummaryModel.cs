/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
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
        public string Name { get; set; }
        public string UserEmail { get; set; }
        public bool IsSuspended { get; set; }

        public ClientActorModel(ApplicationUser user)
        {
            UserEmail = user.Email;
            Name = $"{user.FirstName} {user.LastName}";
            IsSuspended = user.IsSuspended;
        }

        public ClientActorModel(SftpAccount account)
        {
            Name = $"{account.UserName}";
            IsSuspended = account.IsSuspended;
        }

        public ClientActorModel(ClientActorReviewModel user)
        {
            UserEmail = user.UserEmail;
            Name = user.Name;
            IsSuspended = user.IsSuspended;
        }

    }
}
