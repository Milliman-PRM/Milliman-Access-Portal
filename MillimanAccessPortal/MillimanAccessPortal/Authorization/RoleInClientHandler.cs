using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class RoleInClientHandler : AuthorizationHandler<RoleRequirement>
    {
        private ApplicationDbContext DataContext;
        private UserManager<ApplicationUser> UserManager;

        public RoleInClientHandler(ApplicationDbContext DataContextArg,
                                   UserManager<ApplicationUser> UserManagerArg)
        {
            DataContext = DataContextArg;
            UserManager = UserManagerArg;
        }

        /// <summary>
        /// Test the current user for authorization to specified client role
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Requirement">Leave ClientId property unset or &lt; 0 to test for the role authorization to any client</param>
        /// <returns></returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext Context, RoleRequirement Requirement)
        {
            ApplicationUser User = UserManager.GetUserAsync(Context.User).Result;

            IQueryable<UserAuthorizationToClient> Query;

            if (Requirement.ClientId > 0)
            {
                Query = DataContext
                        .UserRoleForClient
                        .Include(urc => urc.Role)
                        .Where(urc => urc.UserId == User.Id &&
                                      urc.ClientId == Requirement.ClientId &&
                                      urc.Role.RoleEnum == Requirement.RoleEnum);
            }
            else
            {
                Query = DataContext
                        .UserRoleForClient
                        .Include(urc => urc.Role)
                        .Where(urc => urc.UserId == User.Id &&
                                      urc.Role.RoleEnum == Requirement.RoleEnum);
            }

            if (Query.Any())  // Query executes here
            {
                Context.Succeed(Requirement);
            }

            return Task.CompletedTask;
        }
    }
}
