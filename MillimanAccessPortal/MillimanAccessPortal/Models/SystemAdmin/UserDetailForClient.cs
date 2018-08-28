using System;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserDetailForClient
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Employer { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public static explicit operator UserDetailForClient(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDetailForClient
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Employer = user.Employer,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
            };
        }
    }
}
