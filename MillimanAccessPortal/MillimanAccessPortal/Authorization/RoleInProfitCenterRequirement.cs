/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class RoleInProfitCenterRequirement : MapAuthorizationRequirementBase
    {
        private bool EvaluateAny { get; set; }
        private RoleEnum RoleEnum { get; set; }
        private Guid ProfitCenterId { get; set; } = Guid.Empty;

        public RoleInProfitCenterRequirement(RoleEnum RoleEnumArg)
        {
            EvaluateAny = true;
            RoleEnum = RoleEnumArg;
        }

        public RoleInProfitCenterRequirement(RoleEnum RoleEnumArg, Guid? ProfitCenterIdArg)
        {
            EvaluateAny = false;
            RoleEnum = RoleEnumArg;
            ProfitCenterId = ProfitCenterIdArg ?? Guid.Empty;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            if (User.IsSuspended)
            {
                return MapAuthorizationRequirementResult.Fail;
            }

            IQueryable<UserRoleInProfitCenter> Query = 
                DataContext.UserRoleInProfitCenter
                           .Include(urc => urc.Role)
                           .Where(urp => urp.Role.RoleEnum == RoleEnum
                                      && urp.UserId == User.Id);

            if (!EvaluateAny)
            {
                Query = Query.Where(urp => urp.ProfitCenterId == ProfitCenterId);
            }

            if (Query.Any())  // Query executes here
            {
                return MapAuthorizationRequirementResult.Succeed;
            }
            else
            {
                return MapAuthorizationRequirementResult.NotPass;
            }
        }
    }
}
