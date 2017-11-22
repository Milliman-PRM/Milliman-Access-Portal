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
    public class UserInContentItemUserGroupRequirement : MapAuthorizationRequirementBase
    {
        private long ContentItemUserGroupId { get; set; }

        /// <summary>
        /// Constructor; the only way to instantiate this type
        /// </summary>
        /// <param name="RoleEnumArg"></param>
        /// <param name="ClientIdArg">null or &lt;= 0 to evaluate for ANY Client</param>
        public UserInContentItemUserGroupRequirement(long ContentItemUserGroupIdArg)
        {
            ContentItemUserGroupId = ContentItemUserGroupIdArg;
        }

        internal override MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            IQueryable<UserInContentItemUserGroup> Query =
                DataContext.UserInContentItemUserGroup
                           .Where(ug => ug.UserId == User.Id
                                     && ug.ContentItemUserGroupId == ContentItemUserGroupId);

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
