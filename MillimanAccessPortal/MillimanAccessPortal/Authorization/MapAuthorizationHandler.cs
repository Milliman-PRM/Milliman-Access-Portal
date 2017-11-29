/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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
    public class MapAuthorizationHandler : AuthorizationHandler<MapAuthorizationRequirementBase>
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
        /// Test the current user for authorization to the specified requirement using logic provided in the requirement
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Requirement"></param>
        /// <returns></returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext Context, MapAuthorizationRequirementBase Requirement)
        {
            ApplicationUser User = UserManager.GetUserAsync(Context.User).Result;

            switch (Requirement.EvaluateRequirement(User, DataContext))
            {
                case MapAuthorizationRequirementResult.Fail:
                    Context.Fail();
                    break;

                case MapAuthorizationRequirementResult.Succeed:
                    Context.Succeed(Requirement);
                    break;

                case MapAuthorizationRequirementResult.Pass:
                    break;

                case MapAuthorizationRequirementResult.NotPass:
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
