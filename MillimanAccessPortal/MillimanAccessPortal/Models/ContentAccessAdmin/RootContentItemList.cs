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

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class RootContentItemList
    {
        public List<RootContentItemSummary> SummaryList = new List<RootContentItemSummary>();
        public long SelectedRootContentItemId { get; set; } = -1;

        internal static RootContentItemList Build(ApplicationDbContext dbContext, Client client, ApplicationUser User, RoleEnum roleInRootContentItem)
        {
            RootContentItemList model = new RootContentItemList();

            List<RootContentItem> rootContentItems = dbContext.UserRoleInRootContentItem
                .Include(urc => urc.RootContentItem)
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == User.Id)
                .Where(urc => urc.Role.RoleEnum == roleInRootContentItem)
                .OrderBy(urc => urc.RootContentItem.ContentName)
                .Select(urc => urc.RootContentItem)
                .ToList();

            // LINQ's .Distinct() with custom comparer is not supported
            // Sort and compare with last element to avoid quadratic runtime
            var distinctRootContentItems = new List<RootContentItem>();
            foreach (var rootContentItem in rootContentItems)
            {
                if (distinctRootContentItems.LastOrDefault()?.Id == rootContentItem.Id)
                {
                    continue;
                }
                distinctRootContentItems.Add(rootContentItem);
            }

            foreach (var rootContentItem in distinctRootContentItems)
            {
                model.SummaryList.Add(RootContentItemSummary.Build(dbContext, rootContentItem));
            }

            return model;
        }
    }
}
