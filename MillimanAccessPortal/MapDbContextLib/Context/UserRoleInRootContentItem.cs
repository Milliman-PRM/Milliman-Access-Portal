/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular RootContentItem
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserRoleInRootContentItem
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("RootContentItem")]
        public Guid RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        [ForeignKey("Role")]
        public Guid RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
