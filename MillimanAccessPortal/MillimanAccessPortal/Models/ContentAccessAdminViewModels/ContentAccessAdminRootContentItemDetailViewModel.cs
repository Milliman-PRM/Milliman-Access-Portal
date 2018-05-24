/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminRootContentItemDetailViewModel
    {
        public RootContentItem RootContentItemEntity { get; set; }
        public int GroupCount { get; set; }
        public int EligibleUserCount { get; set; }
        public PublicationDetails PublicationDetails { get; set; }

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(ApplicationDbContext DbContext, RootContentItem RootContentItem)
        {
            if (RootContentItem.ContentType == null)
            {
                RootContentItem.ContentType = DbContext.ContentType.Find(RootContentItem.ContentTypeId);
            }

            var latestPublication = DbContext.ContentPublicationRequest
                .Where(crt => crt.RootContentItemId == RootContentItem.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();
            PublicationDetails publicationDetails = (PublicationDetails) latestPublication;

            ContentAccessAdminRootContentItemDetailViewModel Model = new ContentAccessAdminRootContentItemDetailViewModel
            {
                RootContentItemEntity = RootContentItem,
                GroupCount = DbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == RootContentItem.Id)
                    .Count(),
                EligibleUserCount = DbContext.UserRoleInRootContentItem
                    // TODO: Qualify with required role/membership in client
                    .Where(ur => ur.RootContentItemId == RootContentItem.Id)
                    .Where(ur => ur.RoleId == ((long)RoleEnum.ContentUser))
                    .Count(),
                PublicationDetails = publicationDetails,
            };

            return Model;
        }

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(long RootContentId, ApplicationDbContext DbContext)
        {
            RootContentItem Content = DbContext.RootContentItem
                .Include(rci => rci.ContentType)
                .Single(rci => rci.Id == RootContentId);

            return Build(DbContext, Content);
        }
    }
}
