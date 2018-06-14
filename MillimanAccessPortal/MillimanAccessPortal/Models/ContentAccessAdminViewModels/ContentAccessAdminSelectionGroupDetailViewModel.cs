/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionGroupDetailViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<UserInfoViewModel> MemberList { get; set; } = new List<UserInfoViewModel>();
        public ReductionDetails ReductionDetails { get; set; }

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(ApplicationDbContext dbContext, SelectionGroup selectionGroup)
        {
            var latestTask = dbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == selectionGroup.Id)
                    .OrderByDescending(crt => crt.CreateDateTimeUtc)
                    .FirstOrDefault();
            var reductionDetails = ((ReductionDetails) latestTask);

            var model = new ContentAccessAdminSelectionGroupDetailViewModel
            {
                Id = selectionGroup.Id,
                Name = selectionGroup.GroupName,
                ReductionDetails = reductionDetails,
            };

            // Retrieve users that are members of the specified selection group
            List<ApplicationUser> memberClients = dbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroupId == selectionGroup.Id)
                .Select(usg => usg.User)
                .ToList();

            foreach (var memberClient in memberClients)
            {
                UserInfoViewModel memberModel = (UserInfoViewModel) memberClient;
                model.MemberList.Add(memberModel);
            }

            return model;
        }

        internal static ContentAccessAdminSelectionGroupDetailViewModel Build(long selectionGroupId, ApplicationDbContext dbContext)
        {
            SelectionGroup selectionGroup = dbContext.SelectionGroup
                .Single(rci => rci.Id == selectionGroupId);

            return Build(dbContext, selectionGroup);
        }
    }
}
