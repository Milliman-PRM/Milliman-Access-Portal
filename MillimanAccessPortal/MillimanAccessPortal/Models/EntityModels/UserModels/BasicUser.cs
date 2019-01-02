using MapDbContextLib.Identity;
using System;

namespace MillimanAccessPortal.Models.UserModels
{
    public class BasicUser
    {
        public Guid Id { get; set; }
        public bool IsActivated { get; set; }
        public bool IsSuspended { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public static explicit operator BasicUser(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new BasicUser
            {
                Id = user.Id,
                IsActivated = user.EmailConfirmed,
                IsSuspended = user.IsSuspended,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
            };
        }
    }
}
