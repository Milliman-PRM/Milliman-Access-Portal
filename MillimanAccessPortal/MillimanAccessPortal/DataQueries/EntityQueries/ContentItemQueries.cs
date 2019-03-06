using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Provides queries related to root content items.
    /// </summary>
    public class ContentItemQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;

        public ContentItemQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
        }

        #region private queries
        /// <summary>
        /// Find a content item by ID
        /// </summary>
        /// <param name="id">Content item ID</param>
        /// <returns>Content item</returns>
        private BasicContentItem FindContentItem(Guid id)
        {
            var contentItem = _dbContext.RootContentItem
                .Where(i => i.Id == id)
                .Select(i => new BasicContentItem
                {
                    Id = i.Id,
                    ClientId = i.ClientId,
                    ContentTypeId = i.ContentTypeId,
                    IsSuspended = i.IsSuspended,
                    DoesReduce = i.DoesReduce,
                    Name = i.ContentName,
                })
                .SingleOrDefault();

            return contentItem;
        }

        /// <summary>
        /// Select all content items for a client where user has a specific role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <param name="clientId">Client ID</param>
        /// <returns>List of content items</returns>
        private List<BasicContentItem> SelectContentItemsWhereClient(
            ApplicationUser user, RoleEnum role, Guid clientId)
        {
            var contentItems = _dbContext.UserRoleInRootContentItem
                .Where(r => r.UserId == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Where(r => r.RootContentItem.ClientId == clientId)
                .Select(r => r.RootContentItem)
                .Distinct()
                .OrderBy(i => i.ContentName)
                    .ThenBy(i => i.ContentType.Name)
                .Select(i => new BasicContentItem
                {
                    Id = i.Id,
                    ClientId = i.ClientId,
                    ContentTypeId = i.ContentTypeId,
                    IsSuspended = i.IsSuspended,
                    DoesReduce = i.DoesReduce,
                    Name = i.ContentName,
                })
                .ToList();

            return contentItems;
        }

        /// <summary>
        /// Add card stats for each content item in a list
        /// </summary>
        /// <param name="contentItems">List of content items</param>
        /// <returns>List of content items with card stats</returns>
        private List<BasicContentItemWithCardStats> WithCardStats(List<BasicContentItem> contentItems)
        {
            var contentItemsWith = new List<BasicContentItemWithCardStats> { };
            foreach (var contentItem in contentItems)
            {
                var contentItemWith = new BasicContentItemWithCardStats
                {
                    Id = contentItem.Id,
                    ClientId = contentItem.ClientId,
                    ContentTypeId = contentItem.ContentTypeId,
                    IsSuspended = contentItem.IsSuspended,
                    DoesReduce = contentItem.DoesReduce,
                    Name = contentItem.Name,
                };

                contentItemWith.SelectionGroupCount = _dbContext.SelectionGroup
                    .Where(g => g.RootContentItemId == contentItem.Id)
                    .Count();
                contentItemWith.AssignedUserCount = _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroup.RootContentItemId == contentItem.Id)
                    .Count();

                contentItemsWith.Add(contentItemWith);
            }
            return contentItemsWith;
        }
        #endregion

        /// <summary>
        /// Select all content items with card stats for a client where user has a specific role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <param name="clientId">Client ID</param>
        /// <returns>List of content items with card stats</returns>
        internal List<BasicContentItemWithCardStats> SelectContentItemsWithCardStatsWhereClient(
            ApplicationUser user, RoleEnum role, Guid clientId)
        {
            var contentItems = SelectContentItemsWhereClient(user, role, clientId);
            var contentItemsWithStats = WithCardStats(contentItems);

            return contentItemsWithStats;
        }

        /// <summary>
        /// Select a single content item by ID with card stats
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>Content item with card stats</returns>
        internal BasicContentItemWithCardStats SelectContentItemWithCardStats(Guid contentItemId)
        {
            var contentItem = FindContentItem(contentItemId);
            var contentItemWithStats = WithCardStats(new List<BasicContentItem> { contentItem })
                .SingleOrDefault();

            return contentItemWithStats;
        }

        /// <summary>
        /// Select a list of unique content types for content items in a list
        /// </summary>
        /// <param name="contentItemIds">List of content item IDs</param>
        /// <returns>List of content types</returns>
        internal List<BasicContentType> SelectContentTypesContentItemIn(List<Guid> contentItemIds)
        {
            var contentTypes = _dbContext.RootContentItem
                .Where(i => contentItemIds.Contains(i.Id))
                .Select(i => i.ContentType)
                .Distinct()
                .OrderBy(t => t.Name)
                .Select(t => new BasicContentType
                {
                    Id = t.Id,
                    Name = ContentType.ContentTypeString.GetValueOrDefault(t.TypeEnum),
                    CanReduce = t.CanReduce,
                    FileExtensions = t.FileExtensions.ToList(),
                })
                .ToList();

            return contentTypes;
        }
    }
}
