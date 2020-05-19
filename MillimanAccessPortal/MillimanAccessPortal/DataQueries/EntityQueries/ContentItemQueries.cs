using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private async Task<BasicContentItem> FindContentItemAsync(Guid id)
        {
            var contentItem = await _dbContext.RootContentItem
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
                .SingleOrDefaultAsync();

            return contentItem;
        }

        /// <summary>
        /// Select all content items for a client where user has a specific role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <param name="clientId">Client ID</param>
        /// <returns>List of content items</returns>
        private async Task<List<BasicContentItem>> SelectContentItemsWhereClientAsync(ApplicationUser user, RoleEnum role, Guid clientId)
        {
            // This runs on the server
            var contentItems = await _dbContext.UserRoleInRootContentItem
                .Where(r => r.UserId == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Where(r => r.RootContentItem.ClientId == clientId)
                .OrderBy(r => r.RootContentItem)
                    .ThenBy(r => r.RootContentItem.ContentName)
                        .ThenBy(r => r.RootContentItem.ContentType.TypeEnum)
                .Select(r => r.RootContentItem)
                .ToListAsync();

            // The rest runs on the client
            var contentItemModels = contentItems
                .Distinct(new IdPropertyComparer<RootContentItem>())  // normally there won't be duplicates
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

            return contentItemModels;
        }

        /// <summary>
        /// Add card stats for each content item in a list
        /// </summary>
        /// <param name="contentItems">List of content items</param>
        /// <returns>List of content items with card stats</returns>
        private async Task<List<BasicContentItemWithCardStats>> WithCardStatsAsync(List<BasicContentItem> contentItems)
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

                contentItemWith.SelectionGroupCount = await _dbContext.SelectionGroup
                    .Where(g => g.RootContentItemId == contentItem.Id)
                    .CountAsync();
                contentItemWith.AssignedUserCount = await _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroup.RootContentItemId == contentItem.Id)
                    .CountAsync();

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
        internal async Task<List<BasicContentItemWithCardStats>> SelectContentItemsWithCardStatsWhereClientAsync(
            ApplicationUser user, RoleEnum role, Guid clientId)
        {
            var contentItems = await SelectContentItemsWhereClientAsync(user, role, clientId);
            var contentItemsWithStats = await WithCardStatsAsync(contentItems);

            return contentItemsWithStats;
        }

        /// <summary>
        /// Select a single content item by ID with card stats
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>Content item with card stats</returns>
        internal async Task<BasicContentItemWithCardStats> SelectContentItemWithCardStatsAsync(Guid contentItemId)
        {
            var contentItem = await FindContentItemAsync(contentItemId);
            var contentItemWithStats = (await WithCardStatsAsync(new List<BasicContentItem> { contentItem }))
                                        .SingleOrDefault();

            return contentItemWithStats;
        }

        /// <summary>
        /// Select a list of unique content types for content items in a list
        /// </summary>
        /// <param name="contentItemIds">List of content item IDs</param>
        /// <returns>List of content types</returns>
        internal async Task<List<BasicContentType>> SelectContentTypesContentItemInAsync(List<Guid> contentItemIds)
        {
            var contentTypes = await _dbContext.RootContentItem
                .Where(i => contentItemIds.Contains(i.Id))
                .Select(i => i.ContentType)
                .ToListAsync();

            var contentTypeModelList = contentTypes
                .Distinct(new IdPropertyComparer<ContentType>())  // normally there won't be duplicates
                .Select(t => new BasicContentType(t))
                .OrderBy(bt => bt.DisplayName)
                .ToList();

            return contentTypeModelList;
        }

        internal async Task<List<BasicContentType>> GetAllContentTypesAsync()
        {
            return await _dbContext.ContentType
                                   .Select(t => new BasicContentType(t))
                                   .ToListAsync();
        }
    }
}
