/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular ProfitCenter
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Comparer for the above entity class, used to ensure proper operation of `.distinct()` function
    /// </summary>
    public class UserRoleInProfitCenterComparer : IEqualityComparer<UserRoleInProfitCenter>
    {
        public bool Equals(UserRoleInProfitCenter x, UserRoleInProfitCenter y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else
                return x.ProfitCenterId == y.ProfitCenterId && x.RoleId == y.RoleId && x.UserId == y.UserId;
        }

        public int GetHashCode(UserRoleInProfitCenter obj)
        {
            string StringToHash = $"{obj.ProfitCenterId}+{obj.RoleId}+{obj.UserId}";
            return StringToHash.GetHashCode();
        }
    }
}
