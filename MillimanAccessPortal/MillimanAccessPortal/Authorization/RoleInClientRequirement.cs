/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Authorization
{
    public class RoleInClientRequirement : MapAuthorizationRequirementBase
    {
        private bool EvaluateAny { get; set; }
        private RoleEnum RoleEnum { get; set; }
        private Guid ClientId { get; set; } = Guid.Empty;

        public RoleInClientRequirement(RoleEnum RoleEnumArg)
        {
            EvaluateAny = true;
            RoleEnum = RoleEnumArg;
        }

        public RoleInClientRequirement(RoleEnum RoleEnumArg, Guid? ClientIdArg)
        {
            EvaluateAny = false;
            RoleEnum = RoleEnumArg;
            ClientId = ClientIdArg ?? Guid.Empty;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            if (User.IsSuspended)
            {
                return MapAuthorizationRequirementResult.Fail;
            }

            IQueryable<UserRoleInClient> Query = 
                DataContext.UserRoleInClient
                           .Include(urc => urc.Role)
                           .Where(urc => urc.Role.RoleEnum == RoleEnum &&
                                         urc.UserId == User.Id);

            if (!EvaluateAny)
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
