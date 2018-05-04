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

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemSummary
    {
        public long Id { get; set; }
        public string ContentName { get; set; }
        public string ContentTypeName { get; set; }
        public int GroupCount { get; set; }
        public int EligibleUserCount { get; set; }
        public PublicationDetails PublicationDetails { get; set; }

        internal static RootContentItemSummary Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            if (rootContentItem.ContentType == null)
            {
                rootContentItem.ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId);
            }

            var latestPublication = dbContext.ContentPublicationRequest
                .Where(crt => crt.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(crt => crt.CreateDateTime)
                .FirstOrDefault();
            PublicationDetails publicationDetails = PublicationDetails.Build(latestPublication, dbContext);

            RootContentItemSummary model = new RootContentItemSummary
            {
                Id = rootContentItem.Id,
                ContentName = rootContentItem.ContentName,
                ContentTypeName = rootContentItem.ContentType.Name,
                GroupCount = dbContext.SelectionGroup
                    .Where(sg => sg.RootContentItemId == rootContentItem.Id)
                    .Count(),
                EligibleUserCount = dbContext.UserRoleInRootContentItem
                    // TODO: Qualify with required role/membership in client
                    .Where(ur => ur.RootContentItemId == rootContentItem.Id)
                    .Where(ur => ur.RoleId == ((long)RoleEnum.ContentUser))
                    .Count(),
                PublicationDetails = publicationDetails,
            };

            return model;
        }
    }

    public class PublicationDetails
    {
        public UserInfoViewModel User { get; set; }
        public PublicationStatus StatusEnum { get; set; }
        public string StatusName { get; set; }
        public long SelectionGroupId { get; set; } = -1;
        public long RootContentItemId { get; set; }

        public static PublicationDetails Build(ContentPublicationRequest contentPublicationRequest, ApplicationDbContext dbContext)
        {
            if (contentPublicationRequest == null)
            {
                return null;
            }

            List<ReductionStatusEnum> ReductionStatusList = dbContext.ContentReductionTask
                .Where(t => t.ContentPublicationRequestId == contentPublicationRequest.Id)
                .Select(t => t.ReductionStatus)
                .ToList();
            PublicationStatus Status = ContentPublicationRequest.GetPublicationStatus(ReductionStatusList);

            return new PublicationDetails
            {
                User = (UserInfoViewModel) contentPublicationRequest.ApplicationUser,
                StatusEnum = Status,
                StatusName = ContentPublicationRequest.PublicationStatusString[Status],
                RootContentItemId = contentPublicationRequest.RootContentItemId,
            };
        }
    }
}
