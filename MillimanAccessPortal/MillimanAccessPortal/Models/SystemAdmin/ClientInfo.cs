/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapCommonLib;
using MapDbContextLib.Context;
using System;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientInfo : Nestable
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int? UserCount { get; set; }
        public int? RootContentItemCount { get; set; }
        public bool ParentOnly { get; set; } = true;

        public static explicit operator ClientInfo(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new ClientInfo
            {
                Id = client.Id,
                ParentId = client.ParentClientId,
                Name = client.Name,
                Code = client.ClientCode,
            };
        }

        public void QueryRelatedEntityCounts(ApplicationDbContext dbContext, Guid? userId, Guid? profitCenterId)
        {
            if (userId.HasValue)
            {
                // don't count users

                // only count root content items that the specified user can view
                RootContentItemCount = dbContext.UserInSelectionGroup
                    .Where(usg => usg.UserId == userId.Value)
                    .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == Id)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .ToHashSet().Count;
            }
            else if (profitCenterId.HasValue)
            {
                // don't count users

                // don't count root content items
            }
            else
            {
                // count all users and root content items related to the client
                UserCount = dbContext.UserClaims
                    .Where(claim => claim.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(claim => claim.ClaimValue == Id.ToString())
                    .Count();

                RootContentItemCount = dbContext.UserInSelectionGroup
                    .Where(usg => usg.SelectionGroup.RootContentItem.ClientId == Id)
                    .Select(usg => usg.SelectionGroup.RootContentItemId)
                    .ToHashSet().Count;
            }
        }
    }
}
