using MapDbContextLib.Context;
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
            // All selection groups of which user is a member
            var selectionGroups = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == user.Id)
                .Where(usg => !usg.SelectionGroup.IsSuspended)
                .Where(usg => !usg.SelectionGroup.RootContentItem.IsSuspended)
                .Select(usg => usg.SelectionGroup)
                .Include(sg => sg.RootContentItem)
                .ToList();

            var notLive = new List<SelectionGroup>();
            foreach (var selectionGroup in selectionGroups)
            {
                var masterContentFile = selectionGroup.RootContentItem.ContentFilesList.FirstOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
                if (masterContentFile == null)
                {
                    notLive.Add(selectionGroup);
                    continue;
                }
                var fileName = ContentAccessSupport.GenerateContentFileName(masterContentFile, selectionGroup.RootContentItemId);
                var filePath = Path.Combine(Path.GetDirectoryName(masterContentFile.FullPath), fileName);
                if (!File.Exists(filePath))
                {
                    notLive.Add(selectionGroup);
                }
            }
            selectionGroups.RemoveAll(sg => notLive.Contains(sg));

            var clients = selectionGroups
                .Select(sg => sg.RootContentItem.Client)
                .ToHashSet();

            UriBuilder contentUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = "/AuthorizedContent/WebHostedContent",
                Query = $"selectionGroupId=",
            };

            UriBuilder thumbnailUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = "/AuthorizedContent/Thumbnail",
                Query = $"selectionGroupId=",
            };

            UriBuilder userGuideUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
                Path = "/AuthorizedContent/RelatedPdf",
                Query = $"purpose=userguide&selectionGroupId=",
            };

            UriBuilder releaseNotesUrlBuilder = new UriBuilder
            {
                Host = Context.Request.Host.Host,
                Scheme = Context.Request.Scheme,
                Port = Context.Request.Host.Port ?? -1,
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
                        ImageURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "thumbnail"))
                            ? $"{thumbnailUrlBuilder.Uri.AbsoluteUri}{sg.Id}"
                            : null,
                        // must be absolute because it is used in iframe element
                        ContentURL = $"{contentUrlBuilder.Uri.AbsoluteUri}{sg.Id}",
                        UserguideURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "userguide"))
                            ? $"{userGuideUrlBuilder.Uri.AbsoluteUri}{sg.Id}"
                            : null,
                        ReleaseNotesURL = (sg.RootContentItem.ContentFilesList.Any(cf => cf.FilePurpose.ToLower() == "releasenotes"))
                            ? $"{releaseNotesUrlBuilder.Uri.AbsoluteUri}{sg.Id}"
                            : null,
                    }).OrderBy(item => item.Name).ToList(),
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
        public string ImageURL { get; set; }
        public string ContentURL { get; set; }
        public string UserguideURL { get; set; }
        public string ReleaseNotesURL { get; set; }
    }
}
