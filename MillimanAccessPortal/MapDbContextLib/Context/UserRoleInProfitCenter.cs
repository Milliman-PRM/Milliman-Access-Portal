/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular ProfitCenter
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserRoleInProfitCenter
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("ProfitCenter")]
        public long ProfitCenterId { get; set; }
        public ProfitCenter ProfitCenter{ get; set; }

        [ForeignKey("Role")]
        public long RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
