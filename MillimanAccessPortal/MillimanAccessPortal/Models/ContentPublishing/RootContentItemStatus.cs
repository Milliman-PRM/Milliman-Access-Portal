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
                model.Status.Add(summary);
            }

            return model;
        }
    }
}
