using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class ClientRoleRequirement : MapAuthorizationRequirementBase
    {
        /// <summary>
        /// Unset, null, or &lt;= 0 to require test for authorization to ANY client.
        /// </summary>
        public long ClientId { get; set; } = -1;
        public RoleEnum RoleEnum { get; set; }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            IQueryable<UserAuthorizationToClient> Query;

            if (ClientId > 0)
            {
                Query = DataContext
                        .UserRoleForClient
                        .Include(urc => urc.Role)
                        .Where(urc => urc.UserId == User.Id &&
                                      urc.ClientId == ClientId &&
                                      urc.Role.RoleEnum == RoleEnum);
            }
            else
            {
                Query = DataContext
                        .UserRoleForClient
                        .Include(urc => urc.Role)
                        .Where(urc => urc.UserId == User.Id &&
                                      urc.Role.RoleEnum == RoleEnum);
            }

            if (Query.Any())  // Query executes here
            {
                return MapAuthorizationRequirementResult.Succeed;
            }
            else
            {
                return MapAuthorizationRequirementResult.Fail;
            }
        }
    }
}
