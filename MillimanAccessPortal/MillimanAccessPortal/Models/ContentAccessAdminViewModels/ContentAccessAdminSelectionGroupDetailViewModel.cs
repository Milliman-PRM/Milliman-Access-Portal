/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing details of a RootContentItem for use in ContentAccessAdmin
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionGroupDetailViewModel
    {
        public ContentItemUserGroup SelectionGroupEntity { get; set; }
        public List<ContentAccessAdminUserInfoViewModel> MemberList { get; set; } = new List<ContentAccessAdminUserInfoViewModel>();

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(ApplicationDbContext DbContext, ContentItemUserGroup SelectionGroup)
        {
            ContentAccessAdminSelectionGroupDetailViewModel Model = new ContentAccessAdminSelectionGroupDetailViewModel
            {
                SelectionGroupEntity = SelectionGroup
            };

            // Retrieve users that are members of the specified selection group
            List<ApplicationUser> MemberClients = DbContext.UserInContentItemUserGroup
                .Where(uug => uug.ContentItemUserGroupId == SelectionGroup.Id)
                .Select(uug => uug.User)
                .ToList();

            foreach (var MemberClient in MemberClients)
            {
                ContentAccessAdminUserInfoViewModel MemberModel = (ContentAccessAdminUserInfoViewModel) MemberClient;
                Model.MemberList.Add(MemberModel);
            }

            return Model;
        }

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(long SelectionGroupId, ApplicationDbContext DbContext)
        {
            ContentItemUserGroup SelectionGroup = DbContext.ContentItemUserGroup
                .Single(rci => rci.Id == SelectionGroupId);

            return Build(DbContext, SelectionGroup);
        }
    }
}
