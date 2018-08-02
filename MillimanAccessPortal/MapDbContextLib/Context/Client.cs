/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a Client organization
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class Client
    {
        public Client()
        {
            AcceptedEmailDomainList = new string[0];
            AcceptedEmailAddressExceptionList = new string[0];
        }

        [Key]
        [HiddenInput]
        [Display(Name = "Client ID")]
        public long Id { get; set; }

        [Required]
        [Display(Name = "Client Name")]
        public string Name { get; set; }

        [Display(Name = "Client Code")]
        public string ClientCode { get; set; }

        [Display(Name = "Primary Client Contact")]
        public string ContactName { get; set; }

        [Display(Name = "Client Contact Title")]
        public string ContactTitle { get; set; }

        [EmailAddress]
        [Display(Name = "Client Contact Email")]
        public string ContactEmail { get; set; }

        [Phone]
        [Display(Name = "Client Contact Phone")]
        public string ContactPhone { get; set; }

        [Display(Name = "Primary Consultant")]
        public string ConsultantName { get; set; }

        [EmailAddress]
        [Display(Name = "Consultant Email")]
        public string ConsultantEmail { get; set; }

        [Display(Name = "Office")]
        public string ConsultantOffice { get; set; }

        [Required]
        [Display(Name = "Approved Email Domain List")]
        public string[] AcceptedEmailDomainList { get; set; }

        [Required]
        [Display(Name = "Approved Email Address Exception List")]
        public string[] AcceptedEmailAddressExceptionList { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Custom Welcome Text")]
        public string NewUserWelcomeText { get; set; }
        
        [ForeignKey("ParentClient")]
        [HiddenInput]
        [Display(Name = "Parent Client ID")]
        public long? ParentClientId { get; set; }

        public Client ParentClient { get; set; }

        [Required]
        [ForeignKey("ProfitCenter")]
        [Display(Name = "Profit Center")]
        public long ProfitCenterId { get; set; }
        public ProfitCenter ProfitCenter { get; set; }
    }
}
