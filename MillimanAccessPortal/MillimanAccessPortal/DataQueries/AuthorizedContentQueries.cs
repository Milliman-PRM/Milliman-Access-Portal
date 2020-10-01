/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Injectable service that performs database interactions in support of the AuthorizedContentController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AuthorizedContentViewModels;
using MillimanAccessPortal.Models.ContentPublishing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    public class AuthorizedContentQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _appConfig;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthorizedContentQueries(
            ApplicationDbContext dbContextArg,
            IConfiguration appConfigArg,
            UserManager<ApplicationUser> userManagerArg)
        {
            _dbContext = dbContextArg;
            _appConfig = appConfigArg;
            _userManager = userManagerArg;
        }

        public async Task<AuthorizedContentViewModel> GetAuthorizedContentViewModel(HttpContext Context)
            {
            if (!Guid.TryParse(_userManager.GetUserId(Context.User), out Guid userId))
            {
                return null;
            }

            List<SelectionGroup> allMatchingSelectionGroupRecords = await _dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == userId)
                .Where(usg => !string.IsNullOrWhiteSpace(usg.SelectionGroup.ContentInstanceUrl))  // only active groups
                .Where(usg => !usg.SelectionGroup.IsSuspended)
                .Where(usg => !usg.SelectionGroup.RootContentItem.IsSuspended)
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(rc => rc.Client)
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(rc => rc.ContentType)
                .Select(usg => usg.SelectionGroup)
                .ToListAsync();

            // EF does not support server side `.Distinct(IEqualityComparer<T>)` so deduplication is done client side here
            var distinctSelectionGroups = allMatchingSelectionGroupRecords
                .Distinct(new IdPropertyComparer<SelectionGroup>())
                .ToList();

            var clients = distinctSelectionGroups
                .Select(sg => sg.RootContentItem.Client)
                .ToHashSet(new IdPropertyComparer<Client>());

            UriBuilder contentUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.ContentWrapper)}",
                Query = $"selectionGroupId=",
            };

            UriBuilder thumbnailUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.Thumbnail)}",
                Query = $"rootContentItemId=",
            };

            UriBuilder userGuideUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.RelatedPdf)}",
                Query = $"purpose=userguide&selectionGroupId=",
            };

            UriBuilder releaseNotesUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.RelatedPdf)}",
                Query = $"purpose=releasenotes&selectionGroupId=",
            };

            return new AuthorizedContentViewModel
            {
                ItemGroups = clients.Select(c =>
                {
                    bool clientAccessReviewIsExpired = DateTime.UtcNow > c.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(_appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"));

                    ContentItemGroup ContentItemGroupModel = new ContentItemGroup
                    {
                        Id = c.Id,
                        Name = c.Name,
                    };

                    if (clientAccessReviewIsExpired)
                    {
                        ContentItemGroupModel.ClientStatus = "Your client admin is a buffoon and is shirking responsibility";
                    }
                    else
                    {
                        ContentItemGroupModel.Items = distinctSelectionGroups.Where(sg => sg.RootContentItem.ClientId == c.Id).Select(sg => new ContentItem
                        {
                            Id = sg.Id,
                            Name = sg.RootContentItem.ContentName,
                            Description = sg.RootContentItem.Description,
                            ContentTypeEnum = sg.RootContentItem.ContentType.TypeEnum,
                            ImageURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "thumbnail"))
                                    ? $"{thumbnailUrlBuilder.Uri.AbsoluteUri}{sg.RootContentItemId}"
                                    : null,
                            ContentURL = $"{contentUrlBuilder.Uri.AbsoluteUri}{sg.Id}",  // must be absolute because it is used in iframe element
                            UserguideURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "userguide"))
                                    ? $"{userGuideUrlBuilder.Uri.AbsoluteUri}{sg.Id}"  // must be absolute because it is used in iframe element
                                    : null,
                            ReleaseNotesURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "releasenotes"))
                                    ? $"{releaseNotesUrlBuilder.Uri.AbsoluteUri}{sg.Id}"  // must be absolute because it is used in iframe element
                                    : null,
                            AssociatedFiles = sg.RootContentItem.AssociatedFilesList.Select(af =>
                            {
                                AssociatedFilePreviewSummary summary = new AssociatedFilePreviewSummary(af);
                                UriBuilder uri = new UriBuilder
                                {
                                    Scheme = Context.Request.Scheme,
                                    Host = Context.Request.Host.Host,
                                    Port = Context.Request.Host.Port ?? -1,
                                    Path = $"/{nameof(AuthorizedContentController).Replace("Controller", "")}/{nameof(AuthorizedContentController.AssociatedFile)}",
                                    Query = $"selectionGroupId={sg.Id}&fileId={af.Id}",
                                };
                                summary.Link = uri.Uri.AbsoluteUri;  // must be absolute because it is used in iframe element
                                    return summary;
                            }).OrderBy(f => f.SortOrder).ToList(),
                        }).OrderBy(contentItem => contentItem.Name).ToList();
                    }

                    return ContentItemGroupModel;
                })
                .OrderBy(itemGroup => itemGroup.Name).ToList()
            };
        }

    }
}
