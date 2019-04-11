/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user's membership in a usergroup with a particular role
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
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

        [Required]
        public bool DisclaimerAccepted { get; set; }
    }

    public class UserInSelectionGroupEqualityComparer : IEqualityComparer<UserInSelectionGroup>
    {
        public bool Equals(UserInSelectionGroup l, UserInSelectionGroup r)
        {
            if (ReferenceEquals(l, r)) return true;
            if (l is null || r is null) return false;
            return l.SelectionGroupId.Equals(r.SelectionGroupId) && l.UserId.Equals(r.UserId);
        }

        public int GetHashCode(UserInSelectionGroup obj)
        {
            return obj.SelectionGroupId.GetHashCode() - obj.UserId.GetHashCode();
        }
    }
}
