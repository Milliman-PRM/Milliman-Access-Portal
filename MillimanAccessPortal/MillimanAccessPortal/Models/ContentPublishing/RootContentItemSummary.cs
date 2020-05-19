/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemSummary
    {
        public Guid Id { get; set; }
        public string ContentName { get; set; }
        public string ContentTypeName { get; set; }
        public int GroupCount { get; set; }
        public int AssignedUserCount { get; set; }
        public bool IsSuspended { get; set; }
        public List<UserInfoViewModel> EligibleUserList = new List<UserInfoViewModel>();
        public PublicationSummary PublicationDetails { get; set; }

        internal static async Task<RootContentItemSummary> BuildAsync(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            if (rootContentItem.ContentType == null)
            {
                rootContentItem.ContentType = await dbContext.ContentType.FindAsync(rootContentItem.ContentTypeId);
            }

            var latestPublication = await dbContext.ContentPublicationRequest
                .Include(crt => crt.ApplicationUser)
                .Where(crt => crt.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefaultAsync();
            PublicationSummary publicationDetails = await latestPublication.ToSummaryWithQueueInformationAsync(dbContext);

            var model = new RootContentItemSummary
            {
                Id = rootContentItem.Id,
                ContentName = rootContentItem.ContentName,
                ContentTypeName = rootContentItem.ContentType.TypeEnum.GetDisplayNameString(),
                GroupCount = await dbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == rootContentItem.Id)
                    .CountAsync(),
                AssignedUserCount = await dbContext.UserInSelectionGroup
                    .Where(usg => usg.SelectionGroup.RootContentItemId == rootContentItem.Id)
                    .Select(usg => usg.UserId)
                    .Distinct()
                    .CountAsync(),
                IsSuspended = rootContentItem.IsSuspended,
                PublicationDetails = publicationDetails,
            };

            var eligibleUsers = await dbContext.UserRoleInClient
                .Where(role => role.ClientId == rootContentItem.ClientId)
                .Where(role => role.Role.RoleEnum == RoleEnum.ContentUser)
                .Select(role => role.User)
                .ToListAsync();
            foreach (var eligibleUser in eligibleUsers)
            {
                var user = ((UserInfoViewModel) eligibleUser);
                model.EligibleUserList.Add(user);
            }

            return model;
        }
    }
}
