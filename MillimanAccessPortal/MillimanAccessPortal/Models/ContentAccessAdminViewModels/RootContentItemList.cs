/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ContentPublishing;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class RootContentItemList
    {
        public List<RootContentItemSummary> SummaryList = new List<RootContentItemSummary>();
        public long SelectedRootContentItemId { get; set; } = -1;

        internal static RootContentItemList Build(ApplicationDbContext dbContext, Client client, ApplicationUser User)
        {
            RootContentItemList model = new RootContentItemList();

            var rootContentItems = dbContext.UserRoleInRootContentItem
                .Include(urc => urc.RootContentItem)
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == User.Id)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentAccessAdmin)
                .Where(urc => dbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroup.RootContentItemId == urc.RootContentItemId)
                    .Where(crt => crt.ReductionStatus == ReductionStatusEnum.Live)
                    .Count() >= 1)
                .Select(urc => urc.RootContentItem)
                .Distinct()
                .ToList();

            foreach (var rootContentItem in rootContentItems)
            {
                model.SummaryList.Add(RootContentItemSummary.Build(dbContext, rootContentItem));
            }

            return model;
        }
    }
}
