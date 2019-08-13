/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Queries to support ContentPublishingController actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

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

        public ContentPublishingAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            UserQueries userQueries,
            ApplicationDbContext dbContextArg
            )
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
            _userQueries = userQueries;
            _dbContext = dbContextArg;
        }

        internal PublishingPageGlobalModel BuildPublishingPageGlobalModel()
        {
            var typeValues = Enum.GetValues(typeof(ContentAssociatedFileType)).Cast<ContentAssociatedFileType>();
            return new PublishingPageGlobalModel
            {
                ContentAssociatedFileTypes = typeValues
                    .Select(t => new AssociatedFileTypeModel(t))
                    .ToDictionary(f => (int)f.TypeEnum),

                ContentTypes = _dbContext.ContentType
                    .Select(t => new BasicContentType(t))
                    .ToDictionary(t => t.Id),
            };
        }

        internal Dictionary<Guid, BasicClientWithCardStats> GetAuthorizedClients(ApplicationUser user, RoleEnum role)
        {
            List<Client> clients = _dbContext.UserRoleInClient
                                             .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == role)
                                             .Select(urc => urc.Client)
                                             .ToList();
            clients = AddUniqueAncestorClientsNonInclusiveOf(clients);

            List<BasicClientWithCardStats> returnList = new List<BasicClientWithCardStats>();

            foreach (Guid authorizedClientId in clients.Select(c => c.Id))
            {
                returnList.Add(_clientQueries.SelectClientWithCardStats(authorizedClientId, role, user.Id));
            }

            return returnList.ToDictionary(c => c.Id);
        }

        internal List<Client> AddUniqueAncestorClientsNonInclusiveOf(IEnumerable<Client> children)
        {
            HashSet<Client> returnSet = children.ToHashSet(new IdPropertyComparer<Client>());

            foreach (Client client in children)
            {
                FindAncestorClients(client).ForEach(c => returnSet.Add(c));
            }
            return returnSet.ToList();
        }

        private List<Client> FindAncestorClients(Client client)
        {
            List<Client> returnObject = new List<Client>();
            if (client.ParentClientId.HasValue && client.ParentClientId.Value != default)
            {
                Client parent = _dbContext.Client.Find(client.ParentClientId);
                returnObject.Add(parent);
                returnObject.AddRange(FindAncestorClients(parent));
            }
            return returnObject;
        }

        internal RootContentItemsModel BuildRootContentItemsModel(Client client, ApplicationUser user, RoleEnum roleInRootContentItem)
        {
            RootContentItemsModel model = new RootContentItemsModel();

            Claim memberOfThisClient = new Claim(ClaimNames.ClientMembership.ToString(), client.Id.ToString());
            model.ClientStats = new
            {
                code = client.ClientCode,
                contentItemCount = _dbContext.UserRoleInRootContentItem
                                            .Where(r => r.UserId == user.Id && r.Role.RoleEnum == roleInRootContentItem && r.RootContentItem.ClientId == client.Id)
                                            .Select(r => r.RootContentItemId)
                                            .Distinct()
                                            .Count(),
                Id = client.Id.ToString(),
                name = client.Name,
                parentId = client.ParentClientId?.ToString(),
                userCount = _dbContext.UserRoleInClient
                                     .Where(r => r.UserId == user.Id && r.Role.RoleEnum == RoleEnum.ContentUser && r.ClientId == client.Id)
                                     .Select(r => r.UserId)
                                     .Distinct()
                                     .Count(),
            };

            List<RootContentItem> rootContentItems = _dbContext.UserRoleInRootContentItem
                .Where(urc => urc.RootContentItem.ClientId == client.Id)
                .Where(urc => urc.UserId == user.Id)
                .Where(urc => urc.Role.RoleEnum == roleInRootContentItem)
                .OrderBy(urc => urc.RootContentItem.ContentName)
                .Select(urc => urc.RootContentItem)
                .AsEnumerable()
                .Distinct(new IdPropertyComparer<RootContentItem>())
                .ToList();
            List<Guid> contentItemIds = rootContentItems.ConvertAll(c => c.Id);
            foreach (var rootContentItem in rootContentItems)
            {
                var summary = RootContentItemNewSummary.Build(_dbContext, rootContentItem);
                model.ContentItems.Add(rootContentItem.Id, summary);
            }

            model.PublicationQueue = PublicationQueueDetails.BuildQueueForClient(_dbContext, client);

            var publications = _dbContext.ContentPublicationRequest
                                          .Where(r => contentItemIds.Contains(r.RootContentItemId))
                                          .GroupBy(r => r.RootContentItemId,
                                                   (k, g) => g.OrderByDescending(r => r.CreateDateTimeUtc).FirstOrDefault()
                                                  );

            // This is required because a `resultSelector` (2nd expression argument) in GroupBy() can return any type, so no EF cache tracking
            _dbContext.AttachRange(publications); // track in EF cache, including navigation properties
            foreach (var pub in publications)
            {
                model.Publications.Add(pub.Id, (BasicPublication)pub);
            }

            return model;
        }

        internal RootContentItemDetail BuildContentItemDetailModel(RootContentItem rootContentItem)
        {
            var publicationRequest = _dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();
            var contentType = _dbContext.ContentType.Find(rootContentItem.ContentTypeId);

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
                ContentDisclaimer = rootContentItem.ContentDisclaimer,
                IsSuspended = rootContentItem.IsSuspended,
                TypeSpecificDetailObject = default,
            };
            switch (contentType.TypeEnum)
            {
                case ContentTypeEnum.PowerBi:
                    model.TypeSpecificDetailObject = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
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

        /// <summary>
        /// Select the publishing page status model
        /// </summary>
        /// <param name="user"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        internal StatusResponseModel SelectStatus(ApplicationUser user, Guid clientId)
        {
            Client client = _dbContext.Client.Find(clientId);

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
                var summary = RootContentItemNewSummary.Build(_dbContext, rootContentItem);
                model.ContentItems.Add(rootContentItem.Id, summary);
            }

            model.PublicationQueue = PublicationQueueDetails.BuildQueueForClient(_dbContext, client);

            var publications = _dbContext.ContentPublicationRequest
                                          .Where(r => contentItemIds.Contains(r.RootContentItemId))
                                          .GroupBy(r => r.RootContentItemId,
                                                   (k, g) => g.OrderByDescending(r => r.CreateDateTimeUtc).FirstOrDefault()
                                                  );

            // This is required because a `resultSelector` (2nd expression argument) in GroupBy() can return any type, so no EF cache tracking
            _dbContext.AttachRange(publications); // track in EF cache, including navigation properties
            foreach (var pub in publications)
            {
                model.Publications.Add(pub.Id, (BasicPublication)pub);
            }

            return model;
        }
    }
}
