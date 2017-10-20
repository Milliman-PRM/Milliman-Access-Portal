using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class ProfitCenterAuthorizationRequirement : MapAuthorizationRequirementBase
    {
        /// <summary>
        /// Unset or &lt;= 0 to require test for authorization to any ProfitCenter.
        /// </summary>
        public long ProfitCenterId { get; set; } = -1;

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            // TODO Some day, convert this to query through injected UserManager service instead of DataContext, no need to pass that as argument
            IQueryable<IdentityUserClaim<long>> Query = DataContext.UserClaims
                                                                   .Where(c => c.ClaimType == ClaimNames.ProfitCenterManager.ToString());

            if (ProfitCenterId > 0)
            {
                Query = Query
                        .Where(claim => claim.ClaimValue == ProfitCenterId.ToString());
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
