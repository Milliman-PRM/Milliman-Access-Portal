/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.AccountViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionGroupSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<UserInfoViewModel> MemberList { get; set; } = new List<UserInfoViewModel>();
        public ReductionSummary ReductionDetails { get; set; }
        public string RootContentItemName { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsInvalid { get; set; }

        internal static SelectionGroupSummary Build(ApplicationDbContext dbContext, SelectionGroup selectionGroup)
        {
            if (selectionGroup.RootContentItem == null)
            {
                selectionGroup.RootContentItem = dbContext.RootContentItem.Find(selectionGroup.RootContentItemId);
            }

            var latestTask = dbContext.ContentReductionTask
                .Include(crt => crt.ApplicationUser)
                .Where(crt => crt.SelectionGroupId == selectionGroup.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();
            var reductionDetails = latestTask.ToSummaryWithQueueInformation(dbContext);

            var model = new SelectionGroupSummary
            {
                Id = selectionGroup.Id,
                Name = selectionGroup.GroupName,
                ReductionDetails = reductionDetails,
                RootContentItemName = selectionGroup.RootContentItem.ContentName,
                IsSuspended = selectionGroup.IsSuspended,
                IsInvalid = selectionGroup.ContentInstanceUrl == null,
            };

            // Retrieve users that are members of the specified selection group
            List<ApplicationUser> memberClients = dbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroupId == selectionGroup.Id)
                .Select(usg => usg.User)
                .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                .ToList();

            foreach (var memberClient in memberClients)
            {
                UserInfoViewModel memberModel = (UserInfoViewModel) memberClient;
                model.MemberList.Add(memberModel);
            }

            return model;
        }

        internal static SelectionGroupSummary Build(Guid selectionGroupId, ApplicationDbContext dbContext)
        {
            SelectionGroup selectionGroup = dbContext.SelectionGroup
                .Single(rci => rci.Id == selectionGroupId);

            return Build(dbContext, selectionGroup);
        }
    }
}
