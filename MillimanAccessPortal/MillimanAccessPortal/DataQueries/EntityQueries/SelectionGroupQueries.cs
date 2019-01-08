using AuditLogLib.Event;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.IO;
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

        internal async Task<SelectionGroup> CreateReducingSelectionGroup(Guid itemId, string name)
        {
            var group = new SelectionGroup
            {
                RootContentItemId = itemId,
                GroupName = name,
                ContentInstanceUrl = "",
                SelectedHierarchyFieldValueList = new Guid[] { },
                IsMaster = false,
            };
            _dbContext.SelectionGroup.Add(group);

            await _dbContext.SaveChangesAsync();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }
        internal async Task<SelectionGroup> CreateMasterSelectionGroup(Guid itemId, string name)
        {
            var contentItem = await _dbContext.RootContentItem.FindAsync(itemId);
            ContentRelatedFile liveMasterFile = contentItem.ContentFilesList
                .SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
            if (liveMasterFile == null || !File.Exists(liveMasterFile.FullPath))
            {
                return null;
            }
            string contentUrl = Path.GetFileName(liveMasterFile.FullPath);

            var group = new SelectionGroup
            {
                RootContentItem = contentItem,
                GroupName = name,
                ContentInstanceUrl = "",
                SelectedHierarchyFieldValueList = new Guid[] { },
                IsMaster = true,
            };
            group.SetContentUrl(contentUrl);
            _dbContext.SelectionGroup.Add(group);

            await _dbContext.SaveChangesAsync();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }
        internal async Task<SelectionGroup> UpdateSelectionGroupName(Guid groupId, string name)
        {
            var group = await _dbContext.SelectionGroup.FindAsync(groupId);
            group.GroupName = name;

            await _dbContext.SaveChangesAsync();

            return group;
        }
        internal async Task<SelectionGroup> UpdateSelectionGroupUsers(Guid groupId, List<Guid> users)
        {
            var group = await _dbContext.SelectionGroup.FindAsync(groupId);

            #region update
            var currentUsers = await _dbContext.UserInSelectionGroup
                .Where(u => u.SelectionGroupId == groupId)
                .ToListAsync();

            var usersToKeep = currentUsers
                .Where(u => users.Contains(u.UserId))
                .Select(u => u.Id);
            var usersToAdd = users.Except(usersToKeep).Select(uid => new UserInSelectionGroup
            {
                UserId = uid,
                SelectionGroupId = groupId,
            });
            _dbContext.UserInSelectionGroup.AddRange(usersToAdd);

            var usersToRemove = currentUsers
                .Where(u => !users.Contains(u.UserId));
            _dbContext.UserInSelectionGroup.RemoveRange(usersToRemove);
            #endregion

            #region commit and log
            await _dbContext.SaveChangesAsync();
            foreach (var user in usersToAdd)
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserAssigned.ToEvent(group, user.Id));
            }
            foreach (var user in usersToRemove)
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserRemoved.ToEvent(group, user.Id));
            }
            #endregion

            return group;
        }
        internal async Task<SelectionGroup> UpdateSelectionGroupSuspended(Guid id, bool isSuspended)
        {
            var group = await _dbContext.SelectionGroup.FindAsync(id);
            group.IsSuspended = isSuspended;

            await _dbContext.SaveChangesAsync();
            _auditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(group, isSuspended, ""));

            return group;
        }
        internal async Task<SelectionGroup> DeleteSelectionGroup(Guid id)
        {
            var group = await _dbContext.SelectionGroup.FindAsync(id);
            _dbContext.SelectionGroup.Remove(group);

            await _dbContext.SaveChangesAsync();
            _auditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(group));

            return group;
        }
    }
}
