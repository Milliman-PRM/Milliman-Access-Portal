/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemStatus
    {
        public List<PublicationSummary> Status = new List<PublicationSummary>();

        public string StatusMessage = string.Empty;

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
                model.Status.Add(publicationRequest.ToSummaryWithQueueInformation(dbContext));
            }

            return model;
        }
    }
}
