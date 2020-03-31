using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MillimanAccessPortal.Models.ContentPublishing;
using MillimanAccessPortal.Controllers;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MillimanAccessPortal.Models.AuthorizedContentViewModels
{
    /// <summary>
    /// A POCO class representing each authorized content item to be presented in the content index page
    /// Anything that gets altered about this class must be reflected in StandardQueries.GetAuthorizedUserGroupsAndRoles() and other places
    /// </summary>
    public class AuthorizedContentViewModel
    {
        public static AuthorizedContentViewModel Build(ApplicationDbContext dbContext, ApplicationUser user, HttpContext Context)
        {
            // EF does not support server side `.Distinct(IEqualityComparer<T>)` so deduplication must be done client side, below
            IEnumerable<SelectionGroup> allMatchingSelectionGroupRecords = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == user.Id)
                .Where(usg => !string.IsNullOrWhiteSpace(usg.SelectionGroup.ContentInstanceUrl))  // is active
                .Where(usg => !usg.SelectionGroup.IsSuspended)
                .Where(usg => !usg.SelectionGroup.RootContentItem.IsSuspended)
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(rc => rc.Client)
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(rc => rc.ContentType)
                .Select(usg => usg.SelectionGroup)
                .AsEnumerable();

            // Deduplicate the above result (client side)
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
                Path = $"/{nameof(AuthorizedContentController).Replace("Controller","")}/{nameof(AuthorizedContentController.Thumbnail)}",
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
                ItemGroups = clients.Select(c => new ContentItemGroup
                {
                    Id = c.Id,
                    Name = c.Name,
                    Items = distinctSelectionGroups.Where(sg => sg.RootContentItem.ClientId == c.Id).Select(sg => new ContentItem
                    {
                        Id = sg.Id,
                        Name = sg.RootContentItem.ContentName,
                        Description = sg.RootContentItem.Description,
                        ContentTypeEnum = sg.RootContentItem.ContentType.TypeEnum,
                        ImageURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "thumbnail"))
                            ? $"{thumbnailUrlBuilder.Uri.AbsoluteUri}{sg.RootContentItemId}"
                            : null,
                        // must be absolute because it is used in iframe element
                        ContentURL = $"{contentUrlBuilder.Uri.AbsoluteUri}{sg.Id}",
                        UserguideURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "userguide"))
                            ? $"{userGuideUrlBuilder.Uri.AbsoluteUri}{sg.Id}"
                            : null,
                        ReleaseNotesURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "releasenotes"))
                            ? $"{releaseNotesUrlBuilder.Uri.AbsoluteUri}{sg.Id}"
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
                            summary.Link = uri.Uri.AbsoluteUri;
                            return summary;
                        }).OrderBy(f => f.SortOrder).ToList(),
                    }).OrderBy(contentItem => contentItem.Name).ToList(),
                }).OrderBy(group => group.Name).ToList(),
            };
        }

        public List<ContentItemGroup> ItemGroups { get; set; }
    }

    public class ContentItemGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ContentItem> Items { get; set; }
    }

    public class ContentItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ContentTypeEnum ContentTypeEnum { get; set; }
        public string ImageURL { get; set; }
        public string ContentURL { get; set; }
        public string UserguideURL { get; set; }
        public string ReleaseNotesURL { get; set; }
        public List<AssociatedFilePreviewSummary> AssociatedFiles { get; set; }
    }
}
