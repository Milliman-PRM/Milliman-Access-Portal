using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
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
        public static AuthorizedContentViewModel Build(ApplicationDbContext dbContext, ApplicationUser user)
        {
            // All selection groups of which the current user is a member 
            var selectionGroupsQuery = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == user.Id)
                .Select(usg => usg.SelectionGroup);

            var selectionGroups = selectionGroupsQuery
                .Include(sg => sg.RootContentItem)
                .ToList();
            var clients = selectionGroupsQuery
                .Select(sg => sg.RootContentItem.Client)
                .ToHashSet();

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
                        ImageURL = sg.RootContentItem.ContentFilesList?.SingleOrDefault(f => f.FilePurpose == "Thumbnail")?.FullPath,
                        ContentURL = sg.ContentInstanceUrl,
                        UserguideURL = sg.RootContentItem.ContentFilesList?.SingleOrDefault(f => f.FilePurpose == "UserGuide")?.FullPath,
                        ReleaseNotesURL = sg.RootContentItem.ContentFilesList?.SingleOrDefault(f => f.FilePurpose == "ReleaseNotes")?.FullPath,
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
