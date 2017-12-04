using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class AssignedRoleInfo
    {
        public RoleEnum RoleEnum { get; set; }
        public string RoleDisplayValue { get; set; }
        public bool IsAssigned { get; set; }

        public static explicit operator AssignedRoleInfo(RoleEnum RoleEnumArg)
        {
            return new AssignedRoleInfo
            {
                RoleEnum = RoleEnumArg,
                RoleDisplayValue = ApplicationRole.RoleDisplayNames[RoleEnumArg],
                IsAssigned = false,  // to be assigned externally
            };
        }
    }

    public class UserInfo
    {
        public long Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<AssignedRoleInfo> UserRoles { get; set; } = new List<AssignedRoleInfo>();

        public static explicit operator UserInfo(ApplicationUser U)
        {
            return new UserInfo
            {
                Id = U.Id,
                Email = U.Email,
                FirstName = U.FirstName,
                LastName = U.LastName,
                UserName = U.UserName,
            };
        }
    }

    /// <summary>
    /// This class is required for set subtraction using the IEnumerable.Except() method.  Does not compare role names
    /// </summary>
    class UserInfoEqualityComparer : IEqualityComparer<UserInfo>
    {
        public bool Equals(UserInfo Left, UserInfo Right)
        {
            if (Left == null && Right == null)
                return true;
            else if (Left == null | Right == null)
                return false;
            else if (Left.Id == Right.Id)
                return true;
            else
                return false;
        }

        public int GetHashCode(UserInfo Arg)
        {
            string hCode = Arg.LastName + Arg.FirstName + Arg.Email + Arg.UserName + Arg.Id.ToString();
            return hCode.GetHashCode();
        }
    }

    public class ClientDetailViewModel
    {
        public List<UserInfo> EligibleUsers { get; set; } = new List<UserInfo>();
        public List<UserInfo> AssignedUsers { get; set; } = new List<UserInfo>();
        public Client ClientEntity { get; set; }
    }
}
