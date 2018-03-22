/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionGroupDetailViewModel
    {
        public SelectionGroup SelectionGroupEntity { get; set; }
        public List<ContentAccessAdminUserInfoViewModel> MemberList { get; set; } = new List<ContentAccessAdminUserInfoViewModel>();
        public ReductionDetails ReductionDetails { get; set; }

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(ApplicationDbContext DbContext, SelectionGroup SelectionGroup)
        {
            var latestTask = DbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                    .OrderByDescending(crt => crt.CreateDateTime)
                    .FirstOrDefault();
            ReductionDetails reductionDetails = ((ReductionDetails) latestTask);

            ContentAccessAdminSelectionGroupDetailViewModel Model = new ContentAccessAdminSelectionGroupDetailViewModel
            {
                SelectionGroupEntity = SelectionGroup,
                ReductionDetails = reductionDetails,
            };

            // Retrieve users that are members of the specified selection group
            List<ApplicationUser> MemberClients = DbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroupId == SelectionGroup.Id)
                .Select(usg => usg.User)
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
