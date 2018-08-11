using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

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
            // All selection groups of which the current user is a member 
            var selectionGroupsQuery = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == user.Id)
                .Where(usg => !usg.SelectionGroup.IsSuspended)
                .Select(usg => usg.SelectionGroup);

            var selectionGroups = selectionGroupsQuery
                .Include(sg => sg.RootContentItem)
                .ToList();
            var clients = selectionGroupsQuery
                .Select(sg => sg.RootContentItem.Client)
                .ToHashSet();

            UriBuilder contentUrlBuilder = new UriBuilder
            {
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port.HasValue ? Context.Request.Host.Port.Value : -1,
                Path = "/AuthorizedContent/WebHostedContent",
                Query = $"selectionGroupId=",
            };

            // TODO each of the below UrlBuilders should be conditional on the existence of the related file
            UriBuilder thumbnailUrlBuilder = new UriBuilder
            {
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port.HasValue ? Context.Request.Host.Port.Value : -1,
                Path = "/AuthorizedContent/Thumbnail",
                Query = $"selectionGroupId=",
            };

            UriBuilder userGuideUrlBuilder = new UriBuilder
            {
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port.HasValue ? Context.Request.Host.Port.Value : -1,
                Path = "/AuthorizedContent/RelatedPdf",
                Query = $"purpose=userguide&selectionGroupId=",
            };

            UriBuilder releaseNotesUrlBuilder = new UriBuilder
            {
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port.HasValue ? Context.Request.Host.Port.Value : -1,
                Path = "/AuthorizedContent/RelatedPdf",
                Query = $"purpose=releasenotes&selectionGroupId=",
            };

            return new AuthorizedContentViewModel
            {
                ItemGroups = clients.Select(c => new ContentItemGroup
                {
                    Id = c.Id,
                    Name = c.Name,
                    Items = selectionGroups.Where(sg => sg.RootContentItem.ClientId == c.Id).Select(sg => new ContentItem
                    {
                        Id = sg.Id,
                        Name = sg.RootContentItem.ContentName,
                        Description = sg.RootContentItem.Description,
                        ImageURL = $"{thumbnailUrlBuilder.Uri.AbsoluteUri}{sg.Id}",
                        ContentURL = $"{contentUrlBuilder.Uri.AbsoluteUri}{sg.Id}",  // must be absolute because it is used in iframe element
                        UserguideURL = $"{userGuideUrlBuilder.Uri.AbsoluteUri}{sg.Id}",
                        ReleaseNotesURL = $"{releaseNotesUrlBuilder.Uri.AbsoluteUri}{sg.Id}",
                    }).OrderBy(item => item.Name).ToList(),
                }).OrderBy(group => group.Name).ToList(),
            };
        }

        public List<ContentItemGroup> ItemGroups { get; set; }
    }

    public class ContentItemGroup
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<ContentItem> Items { get; set; }
    }

    public class ContentItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageURL { get; set; }
        public string ContentURL { get; set; }
        public string UserguideURL { get; set; }
        public string ReleaseNotesURL { get; set; }
    }
}
