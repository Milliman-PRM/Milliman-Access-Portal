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
        private RoleEnum RoleEnum { get; set; }
        private Guid RootContentItemId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public RoleInRootContentItemRequirement(RoleEnum RoleEnumArg, Guid? RootContentItemIdArg)
        {
            RootContentItemId = RootContentItemIdArg.HasValue ? RootContentItemIdArg.Value : Guid.Empty;
            RoleEnum = RoleEnumArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            if (User.IsSuspended)
            {
                return MapAuthorizationRequirementResult.Fail;
            }

            IQueryable<UserRoleInRootContentItem> Query =
                DataContext.UserRoleInRootContentItem
                           .Include(urr => urr.Role)
                           .Where(urc => urc.Role.RoleEnum == RoleEnum &&
                                         urc.UserId == User.Id);

            if (RootContentItemId != Guid.Empty)
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
