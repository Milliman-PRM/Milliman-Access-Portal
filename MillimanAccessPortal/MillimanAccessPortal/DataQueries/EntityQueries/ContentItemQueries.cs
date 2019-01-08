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

        private BasicContentItem _findContentItem(Guid id)
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
        private List<BasicContentItem> _selectContentItemsWhereClient(
            ApplicationUser user, RoleEnum role, Guid clientId)
        {
            var contentItems = _dbContext.UserRoleInRootContentItem
                .Where(r => r.UserId == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Where(r => r.RootContentItem.ClientId == clientId)
                .Select(r => r.RootContentItem)
                .Distinct()
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
        private List<BasicContentItemWithStats> _withStats(List<BasicContentItem> items)
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

                itemWith.SelectionGroupCount = _dbContext.SelectionGroup
                    .Where(g => g.RootContentItemId == item.Id)
                    .Count();
                itemWith.AssignedUserCount = _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroup.RootContentItemId == item.Id)
                    .Count();

                itemsWith.Add(itemWith);
            }
            return itemsWith;
        }

        internal List<BasicContentItemWithStats> SelectContentItemsWhereClient(
            ApplicationUser user, RoleEnum role, Guid clientId)
        {
            var contentItems = _selectContentItemsWhereClient(user, role, clientId);
            var contentItemsWithStats = _withStats(contentItems);

            return contentItemsWithStats;
        }
        internal BasicContentItemWithStats SelectContentItemWithStats(Guid id)
        {
            var contentItem = _findContentItem(id);
            var contentItemWithStats = _withStats(new List<BasicContentItem> { contentItem })
                .SingleOrDefault();

            return contentItemWithStats;
        }

        internal List<BasicContentType> SelectContentTypesContentItemIn(List<Guid> contentItemIds)
        {
            var contentTypes = _dbContext.RootContentItem
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
                .ToList();

            return contentTypes;
        }
    }
}
