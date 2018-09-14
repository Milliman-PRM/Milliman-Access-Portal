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
    public class RootContentItemSummary
    {
        public Guid Id { get; set; }
        public string ContentName { get; set; }
        public string ContentTypeName { get; set; }
        public int GroupCount { get; set; }
        public bool IsSuspended { get; set; }
        public bool ReadOnly { get; set; }
        public List<UserInfoViewModel> EligibleUserList = new List<UserInfoViewModel>();
        public PublicationSummary PublicationDetails { get; set; }

        internal static RootContentItemSummary Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            if (rootContentItem.ContentType == null)
            {
                rootContentItem.ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId);
            }

            var blockingStatusList = new List<PublicationStatus>
            {
                PublicationStatus.Validating,
                PublicationStatus.Queued,
                PublicationStatus.Processing,
                PublicationStatus.Processed,
            };

            var latestPublication = dbContext.ContentPublicationRequest
                .Include(crt => crt.ApplicationUser)
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
                IsSuspended = rootContentItem.IsSuspended,
                ReadOnly = dbContext.ContentPublicationRequest
                    .Where(pr => pr.RootContentItemId == rootContentItem.Id)
                    .Where(pr => blockingStatusList.Contains(pr.RequestStatus))
                    .Any(),
                PublicationDetails = publicationDetails,
            };

            var eligibleUsers = dbContext.UserRoleInClient
                .Where(role => role.ClientId == rootContentItem.ClientId)
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
