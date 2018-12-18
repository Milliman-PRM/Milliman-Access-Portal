/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionGroupStatus
    {
        public List<ReductionSummary> Status = new List<ReductionSummary>();

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
                if (reductionTask == null)
                {
                    continue;
                }
                var summary = reductionTask.ToSummaryWithQueueInformation(dbContext);
                if (!string.IsNullOrWhiteSpace(reductionTask.OutcomeMetadata))
                {
                    // Assemble the list of messages for all failed reductions
                    string message;
                    switch (reductionTask.OutcomeMetadataObj.OutcomeReason)
                    {
                        case MapDbReductionTaskOutcomeReason.SelectionForInvalidFieldName:
                            message = "A value in an invalid field was selected.";
                            break;
                        case MapDbReductionTaskOutcomeReason.NoReducedFileCreated:
                            message = "The selected values do not match any data.";
                            break;
                        default:
                            message = "Unexpected error. Please retry the selection update and "
                                + "contact support if the problem persists.";
                            break;
                    }

                    summary.StatusMessage = message;
                }
                model.Status.Add(summary);
            }

            return model;
        }
    }
}
