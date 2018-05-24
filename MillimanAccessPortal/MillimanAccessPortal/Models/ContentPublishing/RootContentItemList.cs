/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemList
    {
        public List<RootContentItemSummary> DetailList = new List<RootContentItemSummary>();
        public long SelectedRootContentItemId { get; set; } = -1;

        internal static RootContentItemList Build(ApplicationDbContext dbContext, Client client, ApplicationUser User)
        {
            RootContentItemList model = new RootContentItemList();

            List<RootContentItem> rootContentItems = dbContext.UserRoleInRootContentItem
                .Include(urc => urc.RootContentItem)
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == User.Id)
                .Select(urc => urc.RootContentItem)
                .Distinct()
                .ToList();

            foreach (var rootContentItem in rootContentItems)
            {
                model.DetailList.Add(RootContentItemSummary.Build(dbContext, rootContentItem));
            }

            return model;
        }
    }
}
