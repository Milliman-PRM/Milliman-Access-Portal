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
    public class UserInSelectionGroupRequirement : MapAuthorizationRequirementBase
    {
        private long SelectionGroupId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public UserInSelectionGroupRequirement(long SelectionGroupIdArg)
        {
            SelectionGroupId = SelectionGroupIdArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            IQueryable<UserInSelectionGroup> Query =
                DataContext.UserInSelectionGroup
                           .Where(ug => ug.UserId == User.Id
                                     && ug.SelectionGroupId == SelectionGroupId);

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
