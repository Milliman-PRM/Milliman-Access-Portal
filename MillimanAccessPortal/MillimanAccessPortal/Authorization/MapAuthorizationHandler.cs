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
    internal class MapAuthorizationHandler : AuthorizationHandler<MapAuthorizationRequirementBase>
    {
        private ApplicationDbContext DataContext;
        private UserManager<ApplicationUser> UserManager;

        public MapAuthorizationHandler(ApplicationDbContext DataContextArg,
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
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext Context, MapAuthorizationRequirementBase Requirement)
        {
            ApplicationUser User = UserManager.GetUserAsync(Context.User).Result;

            switch (Requirement.EvaluateRequirement(User, DataContext))
            {
                case MapAuthorizationRequirementResult.Fail:
                    Context.Fail();
                    break;

                case MapAuthorizationRequirementResult.Pass:
                    break;

                case MapAuthorizationRequirementResult.Succeed:
                    Context.Succeed(Requirement);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
