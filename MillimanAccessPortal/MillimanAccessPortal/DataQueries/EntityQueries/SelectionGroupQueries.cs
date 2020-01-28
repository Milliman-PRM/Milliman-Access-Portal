/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Encapsulates query operations related to selection groups
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
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
        /// <param name="group">BasicSelectionGroup instance from which the return object is built</param>
        /// <returns>Selection group model instance including assigned users</returns>
        private BasicSelectionGroupWithAssignedUsers WithAssignedUsers(BasicSelectionGroup group)
        {
            BasicSelectionGroupWithAssignedUsers groupWith = new BasicSelectionGroupWithAssignedUsers
            {
                Id = group.Id,
                RootContentItemId = group.RootContentItemId,
                IsSuspended = group.IsSuspended,
                IsInactive = group.IsInactive,
                IsMaster = group.IsMaster,
                Name = group.Name,
                AssignedUsers = _dbContext.UserInSelectionGroup
                                          .Where(u => u.SelectionGroupId == group.Id)
                                          .Select(u => u.UserId)
                                          .Distinct()
                                          .ToList(),
            };

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
            
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group, group.RootContentItem, group.RootContentItem.Client)); ;

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
            var contentItem = _dbContext.RootContentItem
                                        .Include(c => c.ContentType)
                                        .Include(c => c.Client)
                                        .Single(t => t.Id == contentItemId);

            string contentFileName = default;
            if (contentItem.ContentType.TypeEnum.LiveContentFileStoredInMap())
            {
                ContentRelatedFile liveMasterFile = contentItem.ContentFilesList
                    .SingleOrDefault(f => f.FilePurpose.ToLower() == "mastercontent");
                if (liveMasterFile == null || !File.Exists(liveMasterFile.FullPath))
                {
                    return null;
                }

                contentFileName = Path.GetFileName(liveMasterFile.FullPath);
            }

            var group = new SelectionGroup
            {
                RootContentItem = contentItem,
                GroupName = name,
                SelectedHierarchyFieldValueList = new Guid[] { },
                IsMaster = true,
            };
            group.SetContentUrl(contentFileName);

            _dbContext.SelectionGroup.Add(group);
            _dbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group, contentItem, contentItem.Client));

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

            if (group.GroupName != name)
            {
                group.GroupName = name;
                _dbContext.SaveChanges();
            }

            return group;
        }

        /// <summary>
        /// Update the assigned users of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="newListOfUserIds">Selection group users</param>
        /// <returns>Selection group</returns>
        internal SelectionGroup UpdateSelectionGroupUsers(Guid selectionGroupId, List<Guid> newListOfUserIds)
        {
            var requestedGroup = _dbContext.SelectionGroup
                .Include(g => g.RootContentItem)
                    .ThenInclude(c => c.Client)
                .SingleOrDefault(g => g.Id == selectionGroupId);

            List<ApplicationUser> allCurrentUsers = _dbContext.UserInSelectionGroup
                .Where(uisg => uisg.SelectionGroupId == selectionGroupId)
                .Select(uisg => uisg.User)
                .ToList();

            List<ApplicationUser> updatedUserList = _dbContext.ApplicationUser
                .Where(u => newListOfUserIds.Contains(u.Id))
                .ToList();

            List<ApplicationUser> usersToRemove = allCurrentUsers.Except(updatedUserList).ToList();

            List<UserInSelectionGroup> recordsToAdd = updatedUserList.Except(allCurrentUsers)
                .Select(u => new UserInSelectionGroup { UserId = u.Id, SelectionGroupId = selectionGroupId })
                .ToList();
            _dbContext.UserInSelectionGroup.AddRange(recordsToAdd);

            List<UserInSelectionGroup> recordsToRemove = _dbContext.UserInSelectionGroup
                .Include(usg => usg.User)
                .Include(usg => usg.SelectionGroup)
                .Where(usg => usersToRemove.Select(u => u.Id).Contains(usg.UserId))
                .Where(usg => usg.SelectionGroupId == selectionGroupId)
                .ToList();
            _dbContext.UserInSelectionGroup.RemoveRange(recordsToRemove);

            _dbContext.SaveChanges();

            // audit logging
            _dbContext.AttachRange(recordsToAdd);
            foreach (var userInGroup in recordsToAdd)
            {
                _dbContext.Entry(userInGroup)?.Reference(uig => uig.User)?.Load();  // Load `User` navigation property into EF cache for this context
                _auditLogger.Log(AuditEventType.SelectionGroupUserAssigned.ToEvent(requestedGroup, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, userInGroup.User));
            }
            foreach (var userInGroup in usersToRemove.Distinct(new IdPropertyComparer<ApplicationUser>()))
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserRemoved.ToEvent(requestedGroup, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, userInGroup));
            }
            if (usersToRemove.Any())
            {
                _auditLogger.Log(AuditEventType.ContentDisclaimerAcceptanceReset
                    .ToEvent(recordsToRemove, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, ContentDisclaimerResetReason.UserRemovedFromSelectionGroup));
            }

            return requestedGroup;
        }

        /// <summary>
        /// Update the suspended status of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="isSuspended">Suspended status</param>
        /// <returns>Selection group</returns>
        internal SelectionGroup UpdateSelectionGroupSuspended(Guid selectionGroupId, bool isSuspended)
        {
            var group = _dbContext.SelectionGroup
                                  .Include(sg => sg.RootContentItem)
                                      .ThenInclude(ci => ci.Client)
                                  .Single(sg => sg.Id == selectionGroupId);
            group.IsSuspended = isSuspended;
            _dbContext.SaveChanges();

            _auditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(group, group.RootContentItem, group.RootContentItem.Client, isSuspended));

            return group;
        }

        /// <summary>
        /// Delete a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>Deleted selection group</returns>
        internal SelectionGroup DeleteSelectionGroup(Guid selectionGroupId)
        {
            var group = _dbContext.SelectionGroup
                .Include(g => g.RootContentItem)
                    .ThenInclude(c => c.Client)
                .SingleOrDefault(g => g.Id == selectionGroupId);

            if (group != null)
            {
                _dbContext.SelectionGroup.Remove(group);
                _dbContext.SaveChanges();
                _auditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(group, group.RootContentItem, group.RootContentItem.Client));
            }

            return group;
        }
    }
}
