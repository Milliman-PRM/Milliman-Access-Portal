using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> UserRoles { get; set; } = new List<string>();

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

    public class ClientUserListsViewModel
    {
        public List<UserInfo> EligibleUsers { get; set; } = new List<UserInfo>();
        public List<UserInfo> AssignedUsers { get; set; } = new List<UserInfo>();
    }
}
