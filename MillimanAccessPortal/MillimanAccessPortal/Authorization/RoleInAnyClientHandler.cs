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
    public class RoleInAnyClientHandler : AuthorizationHandler<RoleRequirement>
    {
        private ApplicationDbContext DataContext;
        private UserManager<ApplicationUser> UserManager;

        public RoleInAnyClientHandler(ApplicationDbContext DataContextArg, 
                                      UserManager<ApplicationUser> UserManagerArg)
        {
            DataContext = DataContextArg;
            UserManager = UserManagerArg;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext Context, RoleRequirement Requirement)
        {
            ApplicationUser User = UserManager.GetUserAsync(Context.User).Result;

            if (DataContext
                .UserRoleForClient
                .Include(urc => urc.Role)
                .Any(urc => urc.UserId == User.Id &&
                            urc.Role.RoleEnum == Requirement.RoleEnum)
                )
            {
                Context.Succeed(Requirement);
            }

            return Task.CompletedTask;
        }
    }
}
