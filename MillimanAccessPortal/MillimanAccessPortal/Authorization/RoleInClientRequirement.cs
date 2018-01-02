/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class RoleInClientRequirement : MapAuthorizationRequirementBase
    {
        private RoleEnum RoleEnum { get; set; }
        private long ClientId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public RoleInClientRequirement(RoleEnum RoleEnumArg, long? ClientIdArg)
        {
            ClientId = ClientIdArg.HasValue ? ClientIdArg.Value : -1;
            RoleEnum = RoleEnumArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            IQueryable<UserRoleInClient> Query = 
                DataContext.UserRoleInClient
                           .Include(urc => urc.Role)
                           .Where(urc => urc.Role.RoleEnum == RoleEnum &&
                                         urc.UserId == User.Id);

            if (ClientId > 0)
            {
                Query = Query.Where (urc => urc.ClientId == ClientId);
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
