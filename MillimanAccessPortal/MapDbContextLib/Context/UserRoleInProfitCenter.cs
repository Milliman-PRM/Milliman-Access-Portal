/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular ProfitCenter
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserRoleInProfitCenter
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("ProfitCenter")]
        public Guid ProfitCenterId { get; set; }
        public ProfitCenter ProfitCenter{ get; set; }

        [ForeignKey("Role")]
        public Guid RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
