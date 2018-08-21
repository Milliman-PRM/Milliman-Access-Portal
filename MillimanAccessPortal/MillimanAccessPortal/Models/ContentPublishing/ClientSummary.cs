/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Minimal representation of a client
 * DEVELOPER NOTES:
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models.AccountViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{

    public class ClientSummary : Nestable
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public bool CanManage { get; set; }
        public List<UserInfoViewModel> AssignedUsers { get; set; }
        public long EligibleUserCount { get; set; }
        public long RootContentItemCount { get; set; }
        
        async public static Task<ClientSummary> Build(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ApplicationUser currentUser, Client client, RoleEnum roleInClient)
        {
            var clientDetail = new ClientSummary
            {
                Id = client.Id,
                ParentId = client.ParentClientId,
                Name = client.Name,
                Code = client.ClientCode,
            };

            clientDetail.CanManage = dbContext.UserRoleInClient
                .Where(urc => urc.UserId == currentUser.Id)
                .Where(urc => urc.Role.RoleEnum == roleInClient)
                .Where(urc => urc.ClientId == clientDetail.Id)
                .Any();

            // Don't provide more information than necessary
            if (!clientDetail.CanManage) return clientDetail;

            Claim MembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), clientDetail.Id.ToString());
            IList<ApplicationUser> UsersForClaim = await userManager.GetUsersForClaimAsync(MembershipClaim);
            clientDetail.AssignedUsers = UsersForClaim
                .Select(u => (UserInfoViewModel) u)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.UserName)
                .ToList();

            clientDetail.EligibleUserCount = dbContext.UserRoleInClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentUser)
                .Where(urc => urc.ClientId == clientDetail.Id)
                .ToHashSet()
                .Count();

            clientDetail.RootContentItemCount = dbContext.RootContentItem
                .Where(rci => rci.ClientId == clientDetail.Id)
                .Where(rci => !rci.IsSuspended)
                .Count();

            return clientDetail;
        }
    }
}
