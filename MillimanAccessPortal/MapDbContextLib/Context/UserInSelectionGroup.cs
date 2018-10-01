/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user's membership in a usergroup with a particular role
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserInSelectionGroup
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("SelectionGroup")]
        public Guid SelectionGroupId { get; set; }
        public SelectionGroup SelectionGroup { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
