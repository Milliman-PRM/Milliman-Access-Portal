using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    public class ContentItemQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContentItemQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        private async Task<BasicContentItem> _findContentItem(Guid id)
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
        private async Task<List<BasicContentItem>> _selectContentItemsWhereClient(Guid clientId)
        {
            var contentItems = await _dbContext.RootContentItem
                .Where(i => i.ClientId == clientId)
                .Select(i => new BasicContentItem
                {
                    Id = i.Id,
                    ClientId = i.ClientId,
                    ContentTypeId = i.ContentTypeId,
                    IsSuspended = i.IsSuspended,
                    DoesReduce = i.DoesReduce,
                    Name = i.ContentName,
                })
                .ToListAsync();

            return contentItems;
        }
        private async Task<List<BasicContentItemWithStats>> _withStats(List<BasicContentItem> items)
        {
            var itemsWith = new List<BasicContentItemWithStats> { };
            foreach (var item in items)
            {
                var itemWith = new BasicContentItemWithStats
                {
                    Id = item.Id,
                    ClientId = item.ClientId,
                    ContentTypeId = item.ContentTypeId,
                    IsSuspended = item.IsSuspended,
                    DoesReduce = item.DoesReduce,
                    Name = item.Name,
                };

                itemWith.SelectionGroupCount = await _dbContext.SelectionGroup
                    .Where(g => g.RootContentItemId == item.Id)
                    .CountAsync();
                itemWith.AssignedUserCount = await _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroup.RootContentItemId == item.Id)
                    .CountAsync();

                itemsWith.Add(itemWith);
            }
            return itemsWith;
        }

        internal async Task<List<BasicContentItemWithStats>> SelectContentItemsWhereClient(Guid clientId)
        {
            var contentItems = await _selectContentItemsWhereClient(clientId);
            var contentItemsWithStats = await _withStats(contentItems);

            return contentItemsWithStats;
        }
        internal async Task<BasicContentItemWithStats> SelectContentItemWithStats(Guid id)
        {
            var contentItem = await _findContentItem(id);
            var contentItemWithStats = (await _withStats(new List<BasicContentItem> { contentItem }))
                .SingleOrDefault();

            return contentItemWithStats;
        }

        internal async Task<List<BasicContentType>> SelectContentTypesContentItemIn(List<Guid> contentItemIds)
        {
            var contentTypes = await _dbContext.RootContentItem
                .Where(i => contentItemIds.Contains(i.Id))
                .Select(i => i.ContentType)
                .Distinct()
                .Select(t => new BasicContentType
                {
                    Id = t.Id,
                    Name = t.Name,
                    CanReduce = t.CanReduce,
                    FileExtensions = t.FileExtensions.ToList(),
                })
                .ToListAsync();

            return contentTypes;
        }
    }
}
