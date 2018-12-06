/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemStatus
    {
        public List<PublicationSummary> Status = new List<PublicationSummary>();

        internal static RootContentItemStatus Build(ApplicationDbContext dbContext, ApplicationUser user)
        {
            RootContentItemStatus model = new RootContentItemStatus();

            List<RootContentItem> rootContentItems = dbContext.UserRoleInRootContentItem
                .Where(r => r.UserId == user.Id)
                .Select(r => r.RootContentItem)
                .ToHashSet()
                .ToList();

            foreach (var rootContentItem in rootContentItems)
            {
                var publicationRequest = dbContext.ContentPublicationRequest
                    .Include(r => r.ApplicationUser)
                    .Where(r => r.RootContentItemId == rootContentItem.Id)
                    .OrderByDescending(r => r.CreateDateTimeUtc)
                    .FirstOrDefault();
                var summary = publicationRequest.ToSummaryWithQueueInformation(dbContext);

                // Assemble the list of messages for all failed reductions
                string lineBreak = Environment.NewLine;
                foreach (ReductionTaskOutcomeMetadata taskOutcome in publicationRequest.OutcomeMetadataObj.ReductionTaskFailOutcomeList)
                {
                    switch (taskOutcome.OutcomeReason)
                    {
                        case MapDbReductionTaskOutcomeReason.BadRequest:
                            summary.StatusMessage += $"{lineBreak}Bad request";
                            break;
                        case MapDbReductionTaskOutcomeReason.NoSelectedFieldValues:
                            summary.StatusMessage += $"{lineBreak}No field values are selected";
                            break;
                        case MapDbReductionTaskOutcomeReason.NoSelectedFieldValueMatchInNewContent:
                            summary.StatusMessage += $"{lineBreak}No selected field values match data in the new content file";
                            break;
                        case MapDbReductionTaskOutcomeReason.UnspecifiedError:
                            summary.StatusMessage += $"{lineBreak}Unspecified error in selection group processing";
                            break;
                    }
                }
                summary.StatusMessage.TrimStart($"{lineBreak}".ToCharArray());

                model.Status.Add(summary);
            }

            return model;
        }
    }
}
