/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing details of a RootContentItem for use in ContentAccessAdmin
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminRootContentItemDetailViewModel
    {
        public string ContentName { get; set; }
        public string ContentType { get; set; }
        public bool CanReduce { get; set; }
        public int NumberOfGroups { get; set; }
        public int NumberOfAssignedUsers { get; set; }

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(ApplicationDbContext DbContext, RootContentItem Item)
        {
            if (Item.ContentType == null)
            {
                Item.ContentType = DbContext.ContentType.Find(Item.ContentTypeId);
            }

            // Retrieve related users and groups to populate user and group counts
            List<UserInSelectionGroup> RelatedUsersGroups = DbContext.UserInSelectionGroup
                .Include(usg => usg.SelectionGroup)
                .Where(usg => usg.SelectionGroup.RootContentItemId == Item.Id)
                .ToList();

            // See how many members are in each group
            var GroupMemberCounts = DbContext.UserInSelectionGroup
                .GroupBy(usg => usg.SelectionGroupId)
                .Select(usg => new { SelectionGroupId = usg.Key, Count = usg.Count() });

            // Only include a group in NumberOfGroups if it has more than one member
            // Single-member groups are not treated as groups by the front end
            ContentAccessAdminRootContentItemDetailViewModel Model = new ContentAccessAdminRootContentItemDetailViewModel {
                ContentName = Item.ContentName,
                ContentType = Item.ContentType.Name,
                CanReduce = Item.ContentType.CanReduce,
                NumberOfGroups = RelatedUsersGroups
                    .Where(ug => GroupMemberCounts
                        .Single(gmc => gmc.SelectionGroupId == ug.SelectionGroupId)
                        .Count > 1
                        )
                    .Select(ug => ug.SelectionGroupId)
                    .Distinct()
                    .Count(),
                NumberOfAssignedUsers = RelatedUsersGroups
                    .Select(ug => ug.UserId)
                    .Distinct()
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
