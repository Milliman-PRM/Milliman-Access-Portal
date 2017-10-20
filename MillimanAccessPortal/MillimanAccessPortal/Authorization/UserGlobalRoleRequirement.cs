using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class UserGlobalRoleRequirement : MapAuthorizationRequirementBase
    {
        public RoleEnum RoleEnum { get; set; }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            var Query = DataContext.UserRoles
                                   .Join(DataContext.ApplicationRole, ur => ur.RoleId, ar => ar.Id, (ur, ar) => new { UserRole = ur, AppRole = ar })
                                   .Where(obj => obj.UserRole.UserId == User.Id
                                              && obj.AppRole.RoleEnum == RoleEnum);

            if (Query.Any())  // Query executes here
            {
                return MapAuthorizationRequirementResult.Pass;
            }
            else
            {
                return MapAuthorizationRequirementResult.Fail;
            }
        }
    }
}
