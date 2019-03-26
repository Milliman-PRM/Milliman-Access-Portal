using AuditLogLib.Event;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    /// <summary>
    /// Provides queries related to selection groups.
    /// </summary>
    public class SelectionGroupQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;

        public SelectionGroupQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
        }

        #region private queries
        /// <summary>
        /// Find a selection group by ID 
        /// </summary>
        /// <param name="id">Selection group ID</param>
        /// <returns>Selection group</returns>
        private BasicSelectionGroup FindSelectionGroup(Guid id)
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

        /// <summary>
        /// Add a list of assigned users for a single selection group
        /// </summary>
        /// <param name="group">Selection group</param>
        /// <returns>Selection group with assigned users</returns>
        private BasicSelectionGroupWithAssignedUsers WithAssignedUsers(BasicSelectionGroup group)
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

        /// <summary>
        /// Add a list of assigned users for each selection group in a list
        /// </summary>
        /// <param name="group">List of selection groups</param>
        /// <returns>List of selection groups with assigned users</returns>
        private List<BasicSelectionGroupWithAssignedUsers> WithAssignedUsers(List<BasicSelectionGroup> groups)
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
        #endregion

        /// <summary>
        /// Select all selection groups for a content item
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>List of selection groups</returns>
        internal List<BasicSelectionGroup> SelectSelectionGroupsWhereContentItem(Guid contentItemId)
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

        /// <summary>
        /// Select all selection groups with assigned users for a content item
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>List of selection groups with assigned users</returns>
        internal List<BasicSelectionGroupWithAssignedUsers> SelectSelectionGroupsWithAssignedUsers(Guid contentItemId)
        {
            var selectionGroups = SelectSelectionGroupsWhereContentItem(contentItemId);
            var selectionGroupsWithAssignedUsers = WithAssignedUsers(selectionGroups);

            return selectionGroupsWithAssignedUsers;
        }

        /// <summary>
        /// Select a selection group by ID with assigned users
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of selection groups with assigned users</returns>
        internal BasicSelectionGroupWithAssignedUsers SelectSelectionGroupWithAssignedUsers(Guid selectionGroupId)
        {
            var selectionGroup = FindSelectionGroup(selectionGroupId);
            var selectionGroupsWithAssignedUser = WithAssignedUsers(selectionGroup);

            return selectionGroupsWithAssignedUser;
        }

        /// <summary>
        /// Select a list of selections for a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of selections</returns>
        internal List<Guid> SelectSelectionsWhereSelectionGroup(Guid selectionGroupId)
        {
            var selections = _dbContext.SelectionGroup
                .Where(g => g.Id == selectionGroupId)
                .Select(g => g.SelectedHierarchyFieldValueList)
                .SingleOrDefault();

            return selections?.ToList();
        }

        /// <summary>
        /// Select a list of selections for multiple selection groups
        /// </summary>
        /// <param name="selectionGroupIds">List of selection group IDs</param>
        /// <returns>Dictionary of selection lists</returns>
        internal Dictionary<Guid, List<Guid>> SelectSelectionsWhereSelectionGroupIn(List<Guid> selectionGroupIds)
        {
            var selectionsDict = _dbContext.SelectionGroup
                .Where(g => selectionGroupIds.Contains(g.Id))
                .ToDictionary(g => g.Id, g => g.SelectedHierarchyFieldValueList?.ToList());

            return selectionsDict;
        }

        /// <summary>
        /// Create a reducing selection group
        /// </summary>
        /// <param name="contentItemId">Content item ID under which the new selection group will be created</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>New selection group</returns>
        internal SelectionGroup CreateReducingSelectionGroup(Guid contentItemId, string name)
        {
            var group = new SelectionGroup
            {
                RootContentItemId = contentItemId,
                GroupName = name,
                ContentInstanceUrl = null,
                SelectedHierarchyFieldValueList = new Guid[] { },
                IsMaster = false,
            };
            _dbContext.SelectionGroup.Add(group);

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }

        /// <summary>
        /// Create a master selection group
        /// </summary>
        /// <param name="contentItemId">Content item ID under which the new selection group will be created</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>New selection group</returns>
        internal SelectionGroup CreateMasterSelectionGroup(Guid contentItemId, string name)
        {
            var contentItem = _dbContext.RootContentItem.Find(contentItemId);
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
                SelectedHierarchyFieldValueList = new Guid[] { },
                IsMaster = true,
            };
            group.SetContentUrl(contentUrl);
            _dbContext.SelectionGroup.Add(group);

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group));

            return group;
        }

        /// <summary>
        /// Update the name of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="name">Selection group name</param>
        /// <returns>Selection group</returns>
        internal SelectionGroup UpdateSelectionGroupName(Guid selectionGroupId, string name)
        {
            var group = _dbContext.SelectionGroup.Find(selectionGroupId);
            group.GroupName = name;

            _dbContext.SaveChanges();

            return group;
        }

        /// <summary>
        /// Update the assigned users of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="users">Selection group users</param>
        /// <returns>Selection group</returns>
        internal SelectionGroup UpdateSelectionGroupUsers(Guid selectionGroupId, List<Guid> users)
        {
            var group = _dbContext.SelectionGroup.Find(selectionGroupId);

            #region update
            var currentUsers = _dbContext.UserInSelectionGroup
                .Where(u => u.SelectionGroupId == selectionGroupId)
                .ToList();

            var usersToKeep = currentUsers
                .Where(u => users.Contains(u.UserId))
                .Select(u => u.UserId);
            var usersToAdd = users.Except(usersToKeep).Select(uid => new UserInSelectionGroup
            {
                UserId = uid,
                SelectionGroupId = selectionGroupId,
            });
            _dbContext.UserInSelectionGroup.AddRange(usersToAdd);

            var usersToRemove = currentUsers
                .Where(u => !users.Contains(u.UserId));
            _dbContext.UserInSelectionGroup.RemoveRange(usersToRemove);
            #endregion

            #region commit and log
            _dbContext.SaveChanges();
            foreach (var userInGroup in usersToAdd)
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserAssigned.ToEvent(group, userInGroup.UserId));
            }
            foreach (var userInGroup in usersToRemove)
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserRemoved.ToEvent(group, userInGroup.UserId));
            }
            if (usersToRemove.Any())
            {
                _auditLogger.Log(AuditEventType.ContentDisclaimerAcceptanceReset.ToEvent(usersToRemove.ToList()));
            }
            #endregion

            return group;
        }

        /// <summary>
        /// Update the suspended status of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="isSuspended">Suspended status</param>
        /// <returns>Selection group</returns>
        internal SelectionGroup UpdateSelectionGroupSuspended(Guid selectionGroupId, bool isSuspended)
        {
            var group = _dbContext.SelectionGroup.Find(selectionGroupId);
            group.IsSuspended = isSuspended;

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(group, isSuspended, ""));

            return group;
        }

        /// <summary>
        /// Delete a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>Deleted selection group</returns>
        internal SelectionGroup DeleteSelectionGroup(Guid selectionGroupId)
        {
            var group = _dbContext.SelectionGroup.Find(selectionGroupId);
            _dbContext.SelectionGroup.Remove(group);

            _dbContext.SaveChanges();
            _auditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(group));

            return group;
        }
    }
}
