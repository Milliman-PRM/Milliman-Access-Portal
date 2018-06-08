/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class UserGlobalRoleRequirement : MapAuthorizationRequirementBase
    {
        private RoleEnum RoleEnum { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        public UserGlobalRoleRequirement(RoleEnum RoleEnumArg)
        {
            RoleEnum = RoleEnumArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            var Query = DataContext.UserRoles
                                   .Join(DataContext.ApplicationRole, ur => ur.RoleId, ar => ar.Id, (ur, ar) => new { UserRole = ur, AppRole = ar })
                                   .Where(obj => obj.UserRole.UserId == User.Id
                                              && obj.AppRole.RoleEnum == RoleEnum);

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
