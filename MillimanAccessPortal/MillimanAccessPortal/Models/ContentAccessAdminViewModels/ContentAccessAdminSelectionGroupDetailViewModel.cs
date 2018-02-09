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
        public List<ContentAccessAdminUserInfoViewModel> MemberList { get; set; } = new List<ContentAccessAdminUserInfoViewModel>();
        public string Name { get; set; }

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(ApplicationDbContext DbContext, SelectionGroup SelectionGroup)
        {
            ContentAccessAdminSelectionGroupDetailViewModel Model = new ContentAccessAdminSelectionGroupDetailViewModel();

            Model.Name = SelectionGroup.GroupName;

            // Retrieve users that are members of the specified selection group
            List<ApplicationUser> MemberClients = DbContext.UserInSelectionGroup
                .Where(uug => uug.SelectionGroupId == SelectionGroup.Id)
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
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Single(rci => rci.Id == SelectionGroupId);

            return Build(DbContext, SelectionGroup);
        }
    }
}
