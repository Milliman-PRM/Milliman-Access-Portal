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
    public class RoleInRootContentItemRequirement : MapAuthorizationRequirementBase
    {
        private bool EvaluateAny { get; set; }
        private RoleEnum RoleEnum { get; set; }
        private Guid RootContentItemId { get; set; } = Guid.Empty;

        /// <summary>
        /// User this constructor overload to evaluate whether the user is authorized with the specified role for any RootContentItem
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        public RoleInRootContentItemRequirement(RoleEnum RoleEnumArg)
        {
            EvaluateAny = true;
            RoleEnum = RoleEnumArg;
        }

        /// <summary>
        /// User this constructor overload to evaluate whether the user is authorized with the specified role for the specified RootContentItem
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="RootContentItemIdArg"></param>
        public RoleInRootContentItemRequirement(RoleEnum RoleEnumArg, Guid? RootContentItemIdArg)
        {
            EvaluateAny = false;
            RoleEnum = RoleEnumArg;
            RootContentItemId = RootContentItemIdArg ?? Guid.Empty;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            if (User.IsSuspended)
            {
                return MapAuthorizationRequirementResult.Fail;
            }

            IQueryable<UserRoleInRootContentItem> Query = DataContext.UserRoleInRootContentItem
                                                                     .Where(urc => urc.Role.RoleEnum == RoleEnum &&
                                                                                   urc.UserId == User.Id);

            if (!EvaluateAny)
            {
                Query = Query.Where(urc => urc.RootContentItemId == RootContentItemId);
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
