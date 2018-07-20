/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public static explicit operator UserInfo(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserInfo
            {
                Id = user.Id,
                Name = user.FirstName,
            };
        }
    }
}
