using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MillimanAccessPortal.Authorization
{
    public class ProfitCenterAuthorizationRequirement : MapAuthorizationRequirementBase
    {
        private long ProfitCenterId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="ProfitCenterIdArg">Unset or &lt;= 0 to require test for authorization to any ProfitCenter.</param>
        public ProfitCenterAuthorizationRequirement(long ProfitCenterIdArg)
        {
            ProfitCenterId = ProfitCenterId;
        }

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
                return MapAuthorizationRequirementResult.NotPass;
            }
        }
    }
}
