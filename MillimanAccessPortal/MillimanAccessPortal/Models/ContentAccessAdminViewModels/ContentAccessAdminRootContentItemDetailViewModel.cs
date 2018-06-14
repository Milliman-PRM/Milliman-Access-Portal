/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminRootContentItemDetailViewModel
    {
        public long Id { get; set; }
        public string ContentName { get; set; }
        public string ContentTypeName { get; set; }
        public int GroupCount { get; set; }
        public List<UserInfoViewModel> EligibleUserList = new List<UserInfoViewModel>();
        public PublicationDetails PublicationDetails { get; set; }

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            if (rootContentItem.ContentType == null)
            {
                rootContentItem.ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId);
            }

            var latestPublication = dbContext.ContentPublicationRequest
                .Where(crt => crt.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();
            PublicationDetails publicationDetails = (PublicationDetails) latestPublication;

            var model = new ContentAccessAdminRootContentItemDetailViewModel
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

        internal static ContentAccessAdminRootContentItemDetailViewModel Build(long RootContentId, ApplicationDbContext DbContext)
        {
            RootContentItem Content = DbContext.RootContentItem
                .Include(rci => rci.ContentType)
                .Single(rci => rci.Id == RootContentId);

            return Build(DbContext, Content);
        }
    }
}
