/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a Client organization
 * DEVELOPER NOTES: 
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class Client
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string ClientCode { get; set; }

        public string ContactName { get; set; }

        public string ContactTitle { get; set; }

        public string ContactEmail { get; set; }

        public string ContactPhone { get; set; }

        public string ConsultantName { get; set; }

        public string ConsultantEmail { get; set; }

        public string ConsultantOffice { get; set; }

        public string ProfitCenter { get; set; }

        [Required]
        public string[] AcceptedEmailDomainList { get; set; }

        public string[] AcceptedEmailAddressExceptionList { get; set; }
        
        [ForeignKey("ParentClient")]
        public long? ParentClientId { get; set; }
        public Client ParentClient { get; set; }
    }
}
