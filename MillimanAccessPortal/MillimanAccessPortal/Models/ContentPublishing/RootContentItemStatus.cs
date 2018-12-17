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
                var messages = new List<string> { };
                foreach (var taskOutcome in publicationRequest.OutcomeMetadataObj.ReductionTaskFailOutcomeList)
                {
                    switch (taskOutcome.OutcomeReason)
                    {
                        case MapDbReductionTaskOutcomeReason.SelectionForInvalidFieldName:
                            messages.Add("A value in an invalid field was selected.");
                            break;
                        case MapDbReductionTaskOutcomeReason.NoSelectedFieldValues:
                        case MapDbReductionTaskOutcomeReason.NoSelectedFieldValueMatchInNewContent:
                            // these reasons do not contribute to error status
                            break;
                        case MapDbReductionTaskOutcomeReason.BadRequest:
                        case MapDbReductionTaskOutcomeReason.UnspecifiedError:
                        default:
                            // these reasons won't mean anything to a user but could help us
                            messages.Add("Unexpected error. Please retry the publication and "
                                + "contact support if the problem persists.");
                            break;
                    }
                }

                // don't overwhelm the user with a giant error message
                summary.StatusMessage = messages.FirstOrDefault();

                model.Status.Add(summary);
            }

            return model;
        }
    }
}
