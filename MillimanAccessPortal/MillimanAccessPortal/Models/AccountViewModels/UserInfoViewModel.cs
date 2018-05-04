/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserInfoViewModel
    {
        public long Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public static explicit operator UserInfoViewModel(ApplicationUser User)
        {
            return new UserInfoViewModel
            {
                Id = User.Id,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                UserName = User.UserName,
            };
        }
    }
}
