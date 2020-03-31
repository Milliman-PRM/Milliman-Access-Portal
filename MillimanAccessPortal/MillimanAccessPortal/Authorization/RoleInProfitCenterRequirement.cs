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

        /// <summary>
        /// User this constructor overload to evaluate whether the user is authorized with the specified role for any ProfitCenter
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        public RoleInProfitCenterRequirement(RoleEnum RoleEnumArg)
        {
            EvaluateAny = true;
            RoleEnum = RoleEnumArg;
        }

        /// <summary>
        /// User this constructor overload to evaluate whether the user is authorized with the specified role for the specified ProfitCenter
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ProfitCenterIdArg"></param>
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

            IQueryable<UserRoleInProfitCenter> Query = DataContext.UserRoleInProfitCenter
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
