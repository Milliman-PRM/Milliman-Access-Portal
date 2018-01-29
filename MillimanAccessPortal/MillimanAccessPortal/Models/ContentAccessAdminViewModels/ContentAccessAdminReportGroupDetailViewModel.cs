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
    public class ContentAccessAdminReportGroupDetailViewModel
    {
        public List<ContentAccessAdminUserInfoViewModel> MemberList { get; set; } = new List<ContentAccessAdminUserInfoViewModel>();
        public string Name { get; set; }

        internal static ContentAccessAdminReportGroupDetailViewModel Build(ApplicationDbContext DbContext, ContentItemUserGroup ReportGroup)
        {
            ContentAccessAdminReportGroupDetailViewModel Model = new ContentAccessAdminReportGroupDetailViewModel();

            Model.Name = ReportGroup.GroupName;

            // Retrieve users that are members of the specified report group
            List<ApplicationUser> MemberClients = DbContext.UserInContentItemUserGroup
                .Where(u => u.ContentItemUserGroupId == ReportGroup.Id)
                .Select(u => u.User)
                .ToList();

            foreach (var MemberClient in MemberClients)
            {
                ContentAccessAdminUserInfoViewModel MemberModel = (ContentAccessAdminUserInfoViewModel) MemberClient;
                Model.MemberList.Add(MemberModel);
            }

            return Model;
        }

        internal static ContentAccessAdminReportGroupDetailViewModel Build(long ReportGroupId, ApplicationDbContext DbContext)
        {
            ContentItemUserGroup ReportGroup = DbContext.ContentItemUserGroup
                .Single(rci => rci.Id == ReportGroupId);

            return Build(DbContext, ReportGroup);
        }
    }
}
