/*
 * CODE OWNERS: Joseph Sweeney
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
    public class SelectionGroupStatus
    {
        public List<ReductionSummary> Status = new List<ReductionSummary>();

        public string StatusMessage = string.Empty;

        internal static SelectionGroupStatus Build(ApplicationDbContext dbContext, ApplicationUser user)
        {
            SelectionGroupStatus model = new SelectionGroupStatus();

            var rootContentItemIds = dbContext.UserRoleInRootContentItem
                .Where(urc => urc.UserId == user.Id)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentAccessAdmin)
                .Select(urc => urc.RootContentItemId)
                .ToList();
            List<SelectionGroup> selectionGroups = dbContext.SelectionGroup
                .Where(s => rootContentItemIds.Contains(s.RootContentItemId))
                .ToList();

            foreach (var selectionGroup in selectionGroups)
            {
                var reductionTask = dbContext.ContentReductionTask
                    .Include(t => t.ApplicationUser)
                    .Where(t => t.SelectionGroupId == selectionGroup.Id)
                    .OrderByDescending(r => r.CreateDateTimeUtc)
                    .FirstOrDefault();
                model.Status.Add((ReductionSummary) reductionTask);
            }

            return model;
        }
    }
}
