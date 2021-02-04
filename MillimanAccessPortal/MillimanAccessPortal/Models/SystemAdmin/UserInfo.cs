/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide user information for presentation on a user card.
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserInfo
    {
        public Guid Id { get; set; }
        public bool Activated { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsSuspended { get; set; }
        public int? ClientCount { get; set; } = null;
        public int? RootContentItemCount { get; set; } = null;
        public List<RootContentItemInfo> RootContentItems { get; set; }
        public Guid? ProfitCenterId { get; set; } = null;
        public Guid? ClientId { get; set; } = null;
        public DateTime? LastLoginUtc { get; set; }
        public DateTime? AccountDisableDate { get; set; }
        public bool IsAccountDisabled { get; set; } = false;

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
                IsSuspended = user.IsSuspended,
                LastLoginUtc = user.LastLoginUtc,
            };
        }


        public void setAccountDisableStatus(int MonthsToDisableAccount)
        {
            if (this.LastLoginUtc < DateTime.UtcNow.Date.AddMonths(-MonthsToDisableAccount))
            {
                this.IsAccountDisabled = true;
                this.AccountDisableDate = this.LastLoginUtc?.AddMonths(MonthsToDisableAccount);
            }
        }

        public async Task QueryRelatedEntityCountsAsync(ApplicationDbContext dbContext, Guid? clientId, Guid? profitCenterId)
        {
            if (clientId.HasValue)
            {
                ClientId = clientId.Value;

                // don't count clients

                // only count root content items that are under the specified client
                RootContentItemCount = await dbContext.UserInSelectionGroup
                    .Where(usg => usg.UserId == Id)
                    .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == clientId.Value)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .Distinct()
                    .CountAsync();

                _assignRootContentItemList(dbContext, clientId.Value);
            }
            else if (profitCenterId.HasValue)
            {
                ProfitCenterId = profitCenterId.Value;

                // only count clients that are under the specified profit center
                var clientIdList = await dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == Id)
                    .Select(claim => Guid.Parse(claim.ClaimValue))
                    .ToListAsync();
                ClientCount = await dbContext.Client
                    .Where(client => clientIdList.Contains(client.Id))
                    .Where(client => client.ProfitCenterId == profitCenterId.Value)
                    .CountAsync();

                // don't count root content items
            }
            else
            {
                // count all clients and root content items related to the user
                ClientCount = await  dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.UserId == Id)
                    .CountAsync();

                RootContentItemCount = await dbContext.UserInSelectionGroup
                    .Where(usg => usg.UserId == Id)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .Distinct()
                    .CountAsync();
            }
        }

        private void _assignRootContentItemList(ApplicationDbContext dbContext, Guid clientId)
        {
            var query = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == Id)
                .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == clientId)
                .Select(usg => usg.SelectionGroup.RootContentItem);

            var contentItemInfoList = new List<RootContentItemInfo>();
            foreach (var contentItem in query)
            {
                var contentItemInfo = (RootContentItemInfo)contentItem;
                contentItemInfoList.Add(contentItemInfo);
            }

            RootContentItems = contentItemInfoList;
        }
    }
}
