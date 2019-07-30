/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
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

        public Dictionary<Guid, RootContentItemNewSummary> ContentItems { get; set; } = new Dictionary<Guid, RootContentItemNewSummary>();

        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; } = new Dictionary<Guid, PublicationQueueDetails>();

        public Dictionary<Guid, BasicPublication> Publications { get; set; } = new Dictionary<Guid, BasicPublication>();

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
                .AsEnumerable()
                .Distinct(new IdPropertyComparer<RootContentItem>())
                .ToList();
            List<Guid> contentItemIds = rootContentItems.ConvertAll(c => c.Id);
            foreach (var rootContentItem in rootContentItems)
            {
                var summary = RootContentItemNewSummary.Build(dbContext, rootContentItem);
                model.ContentItems.Add(rootContentItem.Id, summary);
            }

            model.PublicationQueue = PublicationQueueDetails.BuildQueueForClient(dbContext, client);

            var publications = dbContext.ContentPublicationRequest
                                          .Where(r => contentItemIds.Contains(r.RootContentItemId))
                                          .Include(r => r.ApplicationUser)
                                          .GroupBy(r => r.RootContentItemId, (k,g) => g.OrderByDescending(r => r.CreateDateTimeUtc).FirstOrDefault());
            // This loop is required because GroupBy aggregation above does not return tracked entities
            foreach (var pub in publications)
            {
                dbContext.Attach(pub); // loads navigation properties
                model.Publications.Add(pub.Id, (BasicPublication)pub);
            }

            return model;
        }
    }
}
