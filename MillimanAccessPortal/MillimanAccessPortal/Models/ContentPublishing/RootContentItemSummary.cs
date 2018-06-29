/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemSummary
    {
        public long Id { get; set; }
        public string ContentName { get; set; }
        public string ContentTypeName { get; set; }
        public int GroupCount { get; set; }
        public List<UserInfoViewModel> EligibleUserList = new List<UserInfoViewModel>();
        public PublicationSummary PublicationDetails { get; set; }

        internal static RootContentItemSummary Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            if (rootContentItem.ContentType == null)
            {
                rootContentItem.ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId);
            }

            var latestPublication = dbContext.ContentPublicationRequest
                .Where(crt => crt.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();
            PublicationSummary publicationDetails = (PublicationSummary) latestPublication;

            var model = new RootContentItemSummary
            {
                Id = rootContentItem.Id,
                ContentName = rootContentItem.ContentName,
                ContentTypeName = rootContentItem.ContentType.Name,
                GroupCount = dbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == rootContentItem.Id)
                    .Count(),
                PublicationDetails = publicationDetails,
            };

            var eligibleUsers = dbContext.UserRoleInRootContentItem
                .Where(role => role.RootContentItemId == rootContentItem.Id)
                .Where(role => role.Role.RoleEnum == RoleEnum.ContentUser)
                .Select(role => role.User)
                .ToList();
            foreach (var eligibleUser in eligibleUsers)
            {
                var user = ((UserInfoViewModel) eligibleUser);
                model.EligibleUserList.Add(user);
            }

            return model;
        }
    }
}
