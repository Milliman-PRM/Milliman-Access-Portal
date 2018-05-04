/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemList
    {
        public List<RootContentItemSummary> DetailList = new List<RootContentItemSummary>();
        public long SelectedRootContentItemId { get; set; } = -1;

        internal static RootContentItemList Build(ApplicationDbContext dbContext, Client client)
        {
            RootContentItemList model = new RootContentItemList();

            List<RootContentItem> rootContentItems = dbContext.RootContentItem
                .Where(rci => rci.ClientId == client.Id)
                .ToList();

            foreach (var rootContentItem in rootContentItems)
            {
                model.DetailList.Add(RootContentItemSummary.Build(dbContext, rootContentItem));
            }

            return model;
        }
    }
}
