/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Queries to support ContentPublishingController actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by content publishing admin actions
    /// </summary>
    public class ContentPublishingAdminQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly UserQueries _userQueries;
        private readonly PublicationQueries _publicationQueries;

        public ContentPublishingAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            UserQueries userQueries,
            ApplicationDbContext dbContextArg,
            PublicationQueries publicationQueriesArg
            )
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
            _userQueries = userQueries;
            _dbContext = dbContextArg;
            _publicationQueries = publicationQueriesArg;
        }

        internal async Task<PublishingPageGlobalModel> BuildPublishingPageGlobalModelAsync()
        {
            var typeValues = Enum.GetValues(typeof(ContentAssociatedFileType)).Cast<ContentAssociatedFileType>();
            return new PublishingPageGlobalModel
            {
                ContentAssociatedFileTypes = typeValues
                    .Select(t => new AssociatedFileTypeModel(t))
                    .ToDictionary(f => (int)f.TypeEnum),

                ContentTypes = await _dbContext.ContentType
                                               .Select(t => new BasicContentType(t))
                                               .ToDictionaryAsync(t => t.Id),
            };
        }

        internal async Task<Dictionary<Guid, BasicClientWithCardStats>> GetAuthorizedClientsModelAsync(ApplicationUser user)
        {
            List<Client> clientList = await _dbContext.UserRoleInClient
                                                      .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == RoleEnum.ContentPublisher)
                                                      .Select(urc => urc.Client)
                                                      .ToListAsync();
            clientList = _clientQueries.AddUniqueAncestorClientsNonInclusiveOf(clientList);

            List<BasicClientWithCardStats> returnList = new List<BasicClientWithCardStats>();

            foreach (Client oneClient in clientList)
            {
                returnList.Add(await _clientQueries.SelectClientWithPublishingCardStatsAsync(oneClient, RoleEnum.ContentPublisher, user.Id));
            }

            return returnList.ToDictionary(c => c.Id);
        }

        /// <summary>
        /// The returned model is identical to the status model but with another property added.  
        /// </summary>
        /// <param name="client"></param>
        /// <param name="user"></param>
        /// <param name="roleInRootContentItem"></param>
        /// <returns></returns>
        internal async Task<RootContentItemsModel> BuildRootContentItemsModelAsync(Client client, ApplicationUser user)
        {
            var statusModel = await SelectStatusAsync(user, client.Id);

            RootContentItemsModel model = new RootContentItemsModel
            {
                ContentItems = statusModel.ContentItems,
                Publications = statusModel.Publications,
                PublicationQueue = statusModel.PublicationQueue,
                ClientStats = new
                {
                    code = client.ClientCode,
                    contentItemCount = await _dbContext.RootContentItem
                                                       .Where(r => r.ClientId == client.Id)
                                                       .Select(r => r.Id)
                                                       .Distinct()
                                                       .CountAsync(),
                    Id = client.Id.ToString(),
                    name = client.Name,
                    parentId = client.ParentClientId?.ToString(),
                    userCount = await _dbContext.UserInSelectionGroup
                                                .Where(g => g.SelectionGroup.RootContentItem.ClientId == client.Id)
                                                .Select(r => r.UserId)
                                                .Distinct()
                                                .CountAsync(),
                },
            };

            return model;
        }

        internal async Task<RootContentItemDetail> BuildContentItemDetailModelAsync(RootContentItem rootContentItem, HttpRequest httpRequest)
        {
            IEnumerable<PublicationStatus> validStatusValues = PublicationStatusExtensions.ActiveStatuses.Append(PublicationStatus.Confirmed);
            ContentPublicationRequest publicationRequest = await _dbContext.ContentPublicationRequest
                                                                           .Where(r => r.RootContentItemId == rootContentItem.Id)
                                                                           .Where(r => validStatusValues.Contains(r.RequestStatus))
                                                                           .OrderByDescending(r => r.CreateDateTimeUtc)
                                                                           .FirstOrDefaultAsync();
            var contentType = await _dbContext.ContentType.FindAsync(rootContentItem.ContentTypeId);

            List<ContentRelatedFile> relatedFiles = rootContentItem.ContentFilesList;
            if ((publicationRequest?.RequestStatus ?? PublicationStatus.Unknown).IsActive())
            {
                var oldFiles = rootContentItem.ContentFilesList;
                var newFiles = publicationRequest.UploadedRelatedFilesObj.Any()
                    ? publicationRequest.UploadedRelatedFilesObj.Select(f => new ContentRelatedFile
                    {
                        FileOriginalName = f.FileOriginalName,
                        FilePurpose = f.FilePurpose,
                    }).ToList()
                    : publicationRequest.LiveReadyFilesObj;
                newFiles.AddRange(oldFiles.Where(f => !newFiles.Select(n => n.FilePurpose).Contains(f.FilePurpose)));
                relatedFiles = newFiles;
            }

            UriBuilder thumbnailUrlBuilder = new UriBuilder
            {
                Host = httpRequest.Host.Host,
                Scheme = httpRequest.Scheme,
                Port = httpRequest.Host.Port ?? -1,
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.Thumbnail)}",
                Query = $"rootContentItemId=",
            };

            var model = new RootContentItemDetail
            {
                Id = rootContentItem.Id,
                ClientId = rootContentItem.ClientId,
                ContentName = rootContentItem.ContentName,
                ContentTypeId = rootContentItem.ContentTypeId,
                DoesReduce = rootContentItem.DoesReduce,
                RelatedFiles = relatedFiles.ToDictionary(f => f.FilePurpose),
                AssociatedFiles = rootContentItem.AssociatedFilesList.ConvertAll(f => new AssociatedFileModel(f)).ToDictionary(f => f.Id),
                ContentDescription = rootContentItem.Description,
                ContentNotes = rootContentItem.Notes,
                ThumbnailLink = (rootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "thumbnail"))
                            ? $"{thumbnailUrlBuilder.Uri.AbsoluteUri}{rootContentItem.Id}"
                            : null,
                ContentDisclaimer = rootContentItem.ContentDisclaimer,
                IsSuspended = rootContentItem.IsSuspended,
                IsEditable = (rootContentItem.ContentType.TypeEnum == ContentTypeEnum.PowerBi &&  (rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties).EditableEnabled),
                TypeSpecificDetailObject = default,
                TypeSpecificPublicationProperties = default,
            };
            switch (contentType.TypeEnum)
            {
                case ContentTypeEnum.PowerBi:
                    model.TypeSpecificDetailObject = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                    if (publicationRequest != null && !String.IsNullOrEmpty(publicationRequest.TypeSpecificDetail))
                    {
                        model.TypeSpecificPublicationProperties = JsonSerializer.Deserialize<PowerBiPublicationProperties>(publicationRequest.TypeSpecificDetail);
                    }
                    break;
                case ContentTypeEnum.Qlikview:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.Html:
                case ContentTypeEnum.FileDownload:
                default:
                    break;
            }

            return model;
        }

        internal async Task<CancelPublicationModel> SelectCancelContentPublicationRequestAsync(ApplicationUser user, RootContentItem rootContentItem, HttpRequest httpRequest)
        {
            var model = new CancelPublicationModel
            {
                StatusResponseModel = await SelectStatusAsync(user, rootContentItem.ClientId),
                RootContentItemDetail = await BuildContentItemDetailModelAsync(rootContentItem, httpRequest),
            };

            return model;
        }

        /// <summary>
        /// Select the publishing page status model
        /// </summary>
        /// <param name="user"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        internal async Task<StatusResponseModel> SelectStatusAsync(ApplicationUser user, Guid clientId)
        {
            Client client = await _dbContext.Client.FindAsync(clientId);

            var model = new StatusResponseModel();

            List<RootContentItem> rootContentItems = _dbContext.UserRoleInRootContentItem
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == user.Id)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentPublisher)
                .OrderBy(urc => urc.RootContentItem.ContentName)
                .Select(urc => urc.RootContentItem)
                .AsEnumerable()
                .Distinct(new IdPropertyComparer<RootContentItem>())
                .ToList();

            List<Guid> contentItemIds = rootContentItems.ConvertAll(c => c.Id);
            foreach (var rootContentItem in rootContentItems)
            {
                var summary = await RootContentItemNewSummary.BuildAsync(_dbContext, rootContentItem);
                model.ContentItems.Add(rootContentItem.Id, summary);
            }

            var publications = await _dbContext.ContentPublicationRequest
                                          .Where(r => contentItemIds.Contains(r.RootContentItemId))
                                          .Where(r => PublicationStatusExtensions.CurrentStatuses.Contains(r.RequestStatus))
                                          .ToListAsync();
            // Only retain the latest one for each content item
            publications = publications.GroupBy(r => r.RootContentItemId,
                                                (k, g) => g.OrderByDescending(r => r.CreateDateTimeUtc).FirstOrDefault()
                                               )
                                       .ToList();

            // This is required because a `resultSelector` (2nd expression argument) in GroupBy() can return any type, so no EF cache tracking
            _dbContext.AttachRange(publications); // track in EF cache, including navigation properties
            foreach (var pub in publications)
            {
                // If the content item has a live publication, only publications requested later than that should be returned
                var livePublication = await _dbContext.ContentPublicationRequest
                                                      .SingleOrDefaultAsync(r => r.RootContentItemId == pub.RootContentItemId
                                                                              && r.RequestStatus == PublicationStatus.Confirmed);

                if (livePublication == null || pub.CreateDateTimeUtc > livePublication.CreateDateTimeUtc)
                {
                    model.Publications.Add(pub.Id, (BasicPublication)pub);
                }
            }

            List<PublicationQueueDetails> publicationQueueModel = await _publicationQueries.SelectQueueDetailsWherePublicationInAsync(model.Publications.Keys);
            model.PublicationQueue = publicationQueueModel.ToDictionary(p => p.PublicationId);

            return model;
        }
    }
}
