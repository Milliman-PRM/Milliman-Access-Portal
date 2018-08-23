using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Authorization
{
    public class RoleInAnySuppliedClientRequirement : MapAuthorizationRequirementBase
    {
        private RoleEnum SuppliedRoleEnum { get; set; }
        private List<long> SuppliedClientList { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public RoleInAnySuppliedClientRequirement(RoleEnum RoleEnumArg, List<long>ClientListArg)
        {
            SuppliedRoleEnum = RoleEnumArg;
            SuppliedClientList = ClientListArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            if (User.IsSuspended)
            {
                return MapAuthorizationRequirementResult.Fail;
            }

            var AuthorizedClientIds = DataContext.UserRoleInClient
                                                 .Include(urc => urc.Role)
                                                 .Where(urc => urc.Role.RoleEnum == SuppliedRoleEnum
                                                            && urc.UserId == User.Id)
                                                 .Select(urc => urc.ClientId)
                                                 .ToList();

            if (AuthorizedClientIds.Intersect(SuppliedClientList).Any())
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
