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
        private RoleEnum RoleEnum { get; set; }
        private Guid ProfitCenterId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="ProfitCenterIdArg">Unset or &lt;= 0 to require test for authorization to any ProfitCenter.</param>
        public RoleInProfitCenterRequirement(RoleEnum RoleEnumArg, Guid? ProfitCenterIdArg)
        {
            ProfitCenterId = ProfitCenterIdArg.HasValue ? ProfitCenterIdArg.Value : Guid.Empty;
            RoleEnum = RoleEnumArg;
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

            if (ProfitCenterId != Guid.Empty)
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
