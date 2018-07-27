using MapDbContextLib.Identity;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserDetailForProfitCenter
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Dictionary<string, List<string>> AssignedClients { get; set; } = null;

        public static explicit operator UserDetailForProfitCenter(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDetailForProfitCenter
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
            };
        }
    }
}
