using AuditLogLib.Event;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private BasicSelectionGroup _findSelectionGroup(Guid id)
        {
            var selectionGroup = _dbContext.SelectionGroup
                .Where(g => g.Id == id)
                .Select(g => new BasicSelectionGroup
                {
                    Id = g.Id,
                    RootContentItemId = g.RootContentItemId,
                    IsSuspended = g.IsSuspended,
                    IsInactive = g.IsInactive,
                    IsMaster = g.IsMaster,
                    Name = g.GroupName,
                })
                .SingleOrDefault();

            return selectionGroup;
        }
        private List<BasicSelectionGroup> _selectSelectionGroupsWhereContentItem(Guid contentItemId)
        {
            var selectionGroups = _dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == contentItemId)
                .OrderBy(g => g.GroupName)
                .Select(g => new BasicSelectionGroup
                {
                    Id = g.Id,
                    RootContentItemId = g.RootContentItemId,
                    IsSuspended = g.IsSuspended,
                    IsInactive = g.IsInactive,
                    IsMaster = g.IsMaster,
                    Name = g.GroupName,
                })
                .ToList();

            return selectionGroups;
        }

        private BasicSelectionGroupWithAssignedUsers _withAssignedUsers(BasicSelectionGroup group)
        {
            var groupWith = new BasicSelectionGroupWithAssignedUsers
            {
                Id = group.Id,
                RootContentItemId = group.RootContentItemId,
                IsSuspended = group.IsSuspended,
                IsInactive = group.IsInactive,
                IsMaster = group.IsMaster,
                Name = group.Name,
            };

            groupWith.AssignedUsers = _dbContext.UserInSelectionGroup
                .Where(u => u.SelectionGroupId == group.Id)
                .Select(u => u.UserId)
                .Distinct()
                .ToList();

            return groupWith;
        }
        private List<BasicSelectionGroupWithAssignedUsers> _withAssignedUsers(
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
                    IsInactive = group.IsInactive,
                    IsMaster = group.IsMaster,
                    Name = group.Name,
                };

                groupWith.AssignedUsers = _dbContext.UserInSelectionGroup
                    .Where(u => u.SelectionGroupId == group.Id)
                    .Select(u => u.UserId)
                    .Distinct()
                    .ToList();

                groupsWith.Add(groupWith);
            }
            return groupsWith;
        }

        internal List<BasicSelectionGroup> SelectSelectionGroupsWhereContentItem(Guid contentItemId)
        {
            var selectionGroups = _selectSelectionGroupsWhereContentItem(contentItemId);

            return selectionGroups;
        }

        internal List<BasicSelectionGroupWithAssignedUsers> SelectSelectionGroupsWithAssignedUsers(
            Guid contentItemId)
        {
            var selectionGroups = _selectSelectionGroupsWhereContentItem(contentItemId);
            var selectionGroupsWithAssignedUsers = _withAssignedUsers(selectionGroups);

            return selectionGroupsWithAssignedUsers;
        }
        internal BasicSelectionGroupWithAssignedUsers SelectSelectionGroupWithAssignedUsers(
            Guid id)
        {
            var selectionGroup = _findSelectionGroup(id);
            var selectionGroupsWithAssignedUser = _withAssignedUsers(selectionGroup);

            return selectionGroupsWithAssignedUser;
        }

        internal List<Guid> SelectSelectionGroupSelections(Guid selectionGroupId)
        {
            var selections = _dbContext.SelectionGroup
                .Where(g => g.Id == selectionGroupId)
                .Select(g => g.SelectedHierarchyFieldValueList)
                .SingleOrDefault();

            return selections?.ToList();
        }

        internal SelectionGroup CreateReducingSelectionGroup(Guid itemId, string name)
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

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }
        internal SelectionGroup CreateMasterSelectionGroup(Guid itemId, string name)
        {
            var contentItem = _dbContext.RootContentItem.Find(itemId);
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

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }
        internal SelectionGroup UpdateSelectionGroupName(Guid groupId, string name)
        {
            var group = _dbContext.SelectionGroup.Find(groupId);
            group.GroupName = name;

            _dbContext.SaveChanges();

            return group;
        }
        internal SelectionGroup UpdateSelectionGroupUsers(Guid groupId, List<Guid> users)
        {
            var group = _dbContext.SelectionGroup.Find(groupId);

            #region update
            var currentUsers = _dbContext.UserInSelectionGroup
                .Where(u => u.SelectionGroupId == groupId)
                .ToList();

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
            _dbContext.SaveChanges();
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
        internal SelectionGroup UpdateSelectionGroupSuspended(Guid id, bool isSuspended)
        {
            var group = _dbContext.SelectionGroup.Find(id);
            group.IsSuspended = isSuspended;

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(group, isSuspended, ""));

            return group;
        }
        internal SelectionGroup DeleteSelectionGroup(Guid id)
        {
            var group = _dbContext.SelectionGroup.Find(id);
            _dbContext.SelectionGroup.Remove(group);

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(group));

            return group;
        }
    }
}
