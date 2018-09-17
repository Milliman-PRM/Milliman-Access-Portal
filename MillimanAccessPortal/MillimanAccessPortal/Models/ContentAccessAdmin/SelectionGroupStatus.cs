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

            List<SelectionGroup> selectionGroups = dbContext.UserInSelectionGroup
                .Where(s => s.UserId == user.Id)
                .Select(s => s.SelectionGroup)
                .ToHashSet()
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
