/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular Client
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserAuthorizationToClient
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("Client")]
        public long ClientId { get; set; }
        public Client Client { get; set; }

        [ForeignKey("Role")]
        public long RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
