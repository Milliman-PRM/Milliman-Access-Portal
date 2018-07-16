using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserList
    {
        public List<UserInfoViewModel> Users { get; set; }

        internal static UserList Build(ApplicationDbContext dbContext, QueryFilter filter)
        {
            var applicationUsers = new List<ApplicationUser>();
            var users = new List<UserInfoViewModel>();

            if (filter.ClientId == null)
            {
                applicationUsers = dbContext.ApplicationUser.ToList();
            }
            else
            {
                var userIds = dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.ClaimValue == filter.ClientId.ToString())
                    .Select(claim => claim.UserId)
                    .ToList();
                applicationUsers = dbContext.ApplicationUser
                    .Where(user => userIds.Contains(user.Id))
                    .ToList();
            }

            foreach (var applicationUser in applicationUsers)
            {
                var user = ((UserInfoViewModel)applicationUser);
                users.Add(user);
            }

            var model = new UserList
            {
                Users = users,
            };

            return model;
        }
    }
}
