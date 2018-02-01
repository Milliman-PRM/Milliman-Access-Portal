/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapCommonLib;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class UserInfoModel
    {
        public long Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public static explicit operator UserInfoModel(ApplicationUser U)
        {
            return new UserInfoModel
            {
                Id = U.Id,
                Email = U.Email,
                FirstName = U.FirstName,
                LastName = U.LastName,
                UserName = U.UserName,
            };
        }
    }

    public class ContentAccessAdminClientDetailViewModel
    {
        public Client ClientEntity { get; set; }
        public List<UserInfoModel> AssignedUsers { get; set; }
        public long EligibleUserCount { get; set; }
        public long RootContentItemCount { get; set; }
        public bool CanManage { get; set; }

        async internal Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser)
        {
            #region Validation
            if (ClientEntity == null)
            {
                throw new MapException("ContentAccessAdminClientDetailViewModel.GenerateSupportingProperties called with no ClientEntity set");
            }
            #endregion

            ClientEntity.ParentClient = null;

            CanManage = DbContext.UserRoleInClient
                .Include(urc => urc.Role)
                .Include(urc => urc.Client)
                .Where(urc => urc.UserId == CurrentUser.Id)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentAccessAdmin)
                .Where(urc => urc.ClientId == ClientEntity.Id)
                .Any();

            // Don't provide more information than necessary
            if (!CanManage) return;

            Claim MembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientEntity.Id.ToString());
            IList<ApplicationUser> UsersForClaim = await UserManager.GetUsersForClaimAsync(MembershipClaim);
            AssignedUsers = UsersForClaim
                .Select(u => (UserInfoModel) u)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.UserName)
                .ToList();

            EligibleUserCount = DbContext.UserRoleInClient
                .Include(urc => urc.Role)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentUser)
                .Where(urc => urc.ClientId == ClientEntity.Id)
                .ToHashSet()
                .Count();

            RootContentItemCount = DbContext.RootContentItem
                .Where(rci => rci.ClientIdList.Contains(ClientEntity.Id))
                .Count();
        }
    }
}
