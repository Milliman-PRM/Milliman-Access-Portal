/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: A ViewModel for MAP
 * DEVELOPER NOTES:
 */

using System.Linq;
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

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(ApplicationDbContext DbContext, RootContentItem RootContentItem)
        {
            if (RootContentItem.ContentType == null)
            {
                RootContentItem.ContentType = DbContext.ContentType.Find(RootContentItem.ContentTypeId);
            }

            ContentAccessAdminRootContentItemDetailViewModel Model = new ContentAccessAdminRootContentItemDetailViewModel {
                RootContentItemEntity = RootContentItem,
                GroupCount = DbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == RootContentItem.Id)
                    .Count(),
                EligibleUserCount = DbContext.UserRoleInRootContentItem
                    // TODO: Qualify with required role/membership in client
                    .Where(ur => ur.RootContentItemId == RootContentItem.Id)
                    .Where(ur => ur.RoleId == ((long) RoleEnum.ContentUser))
                    .Count()
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
