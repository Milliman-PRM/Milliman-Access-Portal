/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserInfoViewModel
    {
        public Guid Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuspended { get; set; } = false;

        public static explicit operator UserInfoViewModel(ApplicationUser User)
        {
            if (User == null)
            {
                return null;
            }

            return new UserInfoViewModel
            {
                Id = User.Id,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                UserName = User.UserName,
                IsSuspended = User.IsSuspended,
            };
        }
    }
}
