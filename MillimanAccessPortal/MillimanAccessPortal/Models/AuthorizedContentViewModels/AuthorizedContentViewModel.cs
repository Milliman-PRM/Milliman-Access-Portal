/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model and components representing a user's authorized content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.AuthorizedContentViewModels
{
    /// <summary>
    /// A POCO class representing each authorized content item to be presented in the content index page
    /// Anything that gets altered about this class must be reflected in StandardQueries.GetAuthorizedUserGroupsAndRoles() and other places
    /// </summary>
    public class AuthorizedContentViewModel
    {
        public List<ContentItemGroup> ItemGroups { get; set; }
    }

    public class ContentItemGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ContentItem> Items { get; set; } = new List<ContentItem>();
        public string ClientStatus { get; set; } = string.Empty;
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
        public TypeSpecificContentItemProperties TypeSpecificDetailObject { get; set; }
        public List<AssociatedFilePreviewSummary> AssociatedFiles { get; set; }
    }
}
