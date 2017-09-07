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

        [Required]
        public string[] AcceptedEmailDomainList { get; set; }

        [ForeignKey("ParentClient")]
        public long? ParentClientId { get; set; }
        public Client ParentClient { get; set; }
    }
}
