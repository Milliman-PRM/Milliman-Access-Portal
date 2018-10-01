/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a user with a particular role for a particular Client
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class UserRoleInClient
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Client")]
        public Guid ClientId { get; set; }
        public Client Client { get; set; }

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
    public class UserRoleInClientComparer : IEqualityComparer<UserRoleInClient>
    {
        public bool Equals(UserRoleInClient x, UserRoleInClient y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else
                return x.ClientId == y.ClientId && x.RoleId == y.RoleId && x.UserId == y.UserId;
        }

        public int GetHashCode(UserRoleInClient obj)
        {
            string StringToHash = $"{obj.ClientId}+{obj.RoleId}+{obj.UserId}";
            return StringToHash.GetHashCode();
        }
    }
}
