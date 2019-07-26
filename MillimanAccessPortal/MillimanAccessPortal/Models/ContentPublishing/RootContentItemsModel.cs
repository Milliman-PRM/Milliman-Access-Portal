/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemsModel
    {
        public object ClientStats { get; set; }

        public Dictionary<Guid, RootContentItemNewSummary> contentItems { get; set; } = new Dictionary<Guid, RootContentItemNewSummary>();

        public Dictionary<Guid, PublicationQueueEntry> publicationQueue { get; set; } = new Dictionary<Guid, PublicationQueueEntry>();

        public Dictionary<Guid, object> publications { get; set; } = new Dictionary<Guid, object>();

        internal static async Task<RootContentItemsModel> BuildAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, Client client, ApplicationUser user, RoleEnum roleInRootContentItem)
        {
            RootContentItemsModel model = new RootContentItemsModel();

            Claim memberOfThisClient = new Claim(ClaimNames.ClientMembership.ToString(), client.Id.ToString());
            model.ClientStats = new
            {
                code = client.ClientCode,
                contentItemCount = dbContext.UserRoleInRootContentItem
                                            .Where(r => r.UserId == user.Id && r.Role.RoleEnum == roleInRootContentItem && r.RootContentItem.ClientId == client.Id)
                                            .Select(r => r.RootContentItem)
                                            .ToList()
                                            .Distinct(new IdPropertyComparer<RootContentItem>())
                                            .Count(),
                Id = client.Id.ToString(),
                name = client.Name,
                parentId = client.ParentClientId.ToString(),
                userCount = (await userManager.GetUsersForClaimAsync(memberOfThisClient)).Select(c => c.UserName).Distinct().Count(),
            };

            List<RootContentItem> rootContentItems = dbContext.UserRoleInRootContentItem
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == user.Id)
                .Where(urc => urc.Role.RoleEnum == roleInRootContentItem)
                .OrderBy(urc => urc.RootContentItem.ContentName)
                .Select(urc => urc.RootContentItem)
                .ToList()
                .Distinct(new IdPropertyComparer<RootContentItem>())
                .ToList();
            foreach (var rootContentItem in rootContentItems)
            {
                var summary = RootContentItemNewSummary.Build(dbContext, rootContentItem);
                model.contentItems.Add(rootContentItem.Id, summary);
            }

            model.publicationQueue = PublicationQueueEntry.Build(dbContext, client);

            return model;
        }
    }
}
