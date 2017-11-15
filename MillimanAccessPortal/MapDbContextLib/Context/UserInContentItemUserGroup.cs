/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user's membership in a usergroup with a particular role
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserInContentItemUserGroup
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("ContentItemUserGroup")]
        public long ContentItemUserGroupId { get; set; }
        public ContentItemUserGroup ContentItemUserGroup { get; set; }

        [ForeignKey("Role")]
        public long RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
