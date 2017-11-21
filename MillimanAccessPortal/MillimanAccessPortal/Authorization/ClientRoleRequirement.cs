using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class ClientRoleRequirement : MapAuthorizationRequirementBase
    {
        private RoleEnum RoleEnum { get; set; }
        private long ClientId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public ClientRoleRequirement(RoleEnum RoleEnumArg, long? ClientIdArg)
        {
            ClientId = ClientIdArg.HasValue ? ClientIdArg.Value : -1;
            RoleEnum = RoleEnumArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            IQueryable<UserRoleInClient> Query;

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
                return MapAuthorizationRequirementResult.NotPass;
            }
        }
    }
}
