using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    public class SelectionGroupQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public SelectionGroupQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        private async Task<BasicSelectionGroup> _findSelectionGroup(Guid id)
        {
            var selectionGroup = await _dbContext.SelectionGroup
                .Where(g => g.Id == id)
                .Select(g => new BasicSelectionGroup
                {
                    Id = g.Id,
                    RootContentItemId = g.RootContentItemId,
                    IsSuspended = g.IsSuspended,
                    IsMaster = g.IsMaster,
                    Name = g.GroupName,
                })
                .SingleOrDefaultAsync();

            return selectionGroup;
        }
        private async Task<List<BasicSelectionGroup>> _selectSelectionGroupsWhereContentItem(Guid contentItemId)
        {
            var selectionGroups = await _dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == contentItemId)
                .Select(g => new BasicSelectionGroup
                {
                    Id = g.Id,
                    RootContentItemId = g.RootContentItemId,
                    IsSuspended = g.IsSuspended,
                    IsMaster = g.IsMaster,
                    Name = g.GroupName,
                })
                .ToListAsync();

            return selectionGroups;
        }

        private async Task<BasicSelectionGroupWithAssignedUsers> _withAssignedUsers(BasicSelectionGroup group)
        {
            var groupWith = new BasicSelectionGroupWithAssignedUsers
            {
                Id = group.Id,
                RootContentItemId = group.RootContentItemId,
                IsSuspended = group.IsSuspended,
                IsMaster = group.IsMaster,
                Name = group.Name,
            };

            groupWith.AssignedUsers = await _dbContext.UserInSelectionGroup
                .Where(u => u.SelectionGroupId == group.Id)
                .Select(u => u.UserId)
                .Distinct()
                .ToListAsync();

            return groupWith;
        }
        private async Task<List<BasicSelectionGroupWithAssignedUsers>> _withAssignedUsers(
            List<BasicSelectionGroup> groups)
        {
            var groupsWith = new List<BasicSelectionGroupWithAssignedUsers> { };
            foreach (var group in groups)
            {
                var groupWith = new BasicSelectionGroupWithAssignedUsers
                {
                    Id = group.Id,
                    RootContentItemId = group.RootContentItemId,
                    IsSuspended = group.IsSuspended,
                    IsMaster = group.IsMaster,
                    Name = group.Name,
                };

                groupWith.AssignedUsers = await _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroupId == group.Id)
                    .Select(u => u.UserId)
                    .Distinct()
                    .ToListAsync();

                groupsWith.Add(groupWith);
            }
            return groupsWith;
        }

        internal async Task<List<BasicSelectionGroup>> SelectSelectionGroupsWhereContentItem(Guid contentItemId)
        {
            var selectionGroups = await _selectSelectionGroupsWhereContentItem(contentItemId);

            return selectionGroups;
        }

        internal async Task<List<BasicSelectionGroupWithAssignedUsers>> SelectSelectionGroupsWithAssignedUsers(
            Guid contentItemId)
        {
            var selectionGroups = await _selectSelectionGroupsWhereContentItem(contentItemId);
            var selectionGroupsWithAssignedUsers = await _withAssignedUsers(selectionGroups);

            return selectionGroupsWithAssignedUsers;
        }
        internal async Task<BasicSelectionGroupWithAssignedUsers> SelectSelectionGroupWithAssignedUsers(
            Guid id)
        {
            var selectionGroup = await _findSelectionGroup(id);
            var selectionGroupsWithAssignedUser = await _withAssignedUsers(selectionGroup);

            return selectionGroupsWithAssignedUser;
        }

        internal async Task<List<Guid>> SelectSelectionGroupSelections(Guid selectionGroupId)
        {
            var selections = await _dbContext.SelectionGroup
                .Where(g => g.Id == selectionGroupId)
                .Select(g => g.SelectedHierarchyFieldValueList)
                .SingleOrDefaultAsync();

            return selections?.ToList();
        }
    }
}
