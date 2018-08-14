/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserInfo
    {
        public long Id { get; set; }
        public bool Activated { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int? ClientCount { get; set; } = null;
        public int? RootContentItemCount { get; set; } = null;
        public List<RootContentItemInfo> RootContentItems { get; set; }

        public static explicit operator UserInfo(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserInfo
            {
                Id = user.Id,
                Activated = user.EmailConfirmed,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
            };
        }

        public void QueryRelatedEntityCounts(ApplicationDbContext dbContext, long? clientId, long? profitCenterId)
        {
            if (clientId.HasValue)
            {
                // don't count clients

                // only count root content items that are under the specified client
                RootContentItemCount = dbContext.UserInSelectionGroup
                    .Where(usg => usg.UserId == Id)
                    .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == clientId.Value)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .ToHashSet().Count;

                _assignRootContentItemList(dbContext);
            }
            else if (profitCenterId.HasValue)
            {
                // only count clients that are under the specified profit center
                var clientIdList = dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == Id)
                    .Select(claim => long.Parse(claim.ClaimValue))
                    .ToList();
                ClientCount = dbContext.Client
                    .Where(client => clientIdList.Contains(client.Id))
                    .Where(client => client.ProfitCenterId == profitCenterId.Value)
                    .Count();

                // don't count root content items
            }
            else
            {
                // count all clients and root content items related to the user
                ClientCount = dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == Id)
                    .Count();

                RootContentItemCount = dbContext.UserInSelectionGroup
                    .Where(usg => usg.UserId == Id)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .ToHashSet().Count;
            }
        }

        private void _assignRootContentItemList(ApplicationDbContext dbContext)
        {
            var query = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == Id)
                .Select(usg => usg.SelectionGroup.RootContentItem);

            var itemInfoList = new List<RootContentItemInfo>();
            foreach (var item in query)
            {
                var itemInfo = (RootContentItemInfo)item;
                itemInfoList.Add(itemInfo);
            }

            RootContentItems = itemInfoList;
        }
    }
}
