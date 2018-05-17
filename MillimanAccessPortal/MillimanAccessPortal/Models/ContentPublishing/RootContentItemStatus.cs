/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.ContentAccessAdminViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemStatus
    {
        public List<PublicationDetails> Status = new List<PublicationDetails>();

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
                    .Where(r => r.RootContentItemId == rootContentItem.Id)
                    .OrderByDescending(r => r.CreateDateTimeUtc)
                    .FirstOrDefault();
                model.Status.Add((PublicationDetails) publicationRequest);
            }

            return model;
        }
    }
}
