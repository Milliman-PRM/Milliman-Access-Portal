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
using Microsoft.AspNetCore.Http;
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
    /// <summary>
    /// Provides queries related to selection groups.
    /// </summary>
    public class SelectionGroupQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SelectionGroupQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessorArg,
            UserManager<ApplicationUser> userManagerArg)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessorArg;
            _userManager = userManagerArg;
        }

        #region private queries
        /// <summary>
        /// Find a selection group by ID 
        /// </summary>
        /// <param name="id">Selection group ID</param>
        /// <returns>A model of the matching SelectionGroup</returns>
        private async Task<BasicSelectionGroup> FindSelectionGroupAsync(Guid id)
        {
            var selectionGroup = await _dbContext.SelectionGroup.SingleOrDefaultAsync(g => g.Id == id);

            var selectionGroupModel = new BasicSelectionGroup
                {
                    Id = selectionGroup.Id,
                    RootContentItemId = selectionGroup.RootContentItemId,
                    IsSuspended = selectionGroup.IsSuspended,
                    IsInactive = string.IsNullOrWhiteSpace(selectionGroup.ContentInstanceUrl),
                    IsMaster = selectionGroup.IsMaster,
                    Name = selectionGroup.GroupName,
                };

            return selectionGroupModel;
        }

        /// <summary>
        /// Add a list of assigned users for a single selection group
        /// </summary>
        /// <param name="group">BasicSelectionGroup instance from which the return object is built</param>
        /// <returns>Selection group model instance including assigned users</returns>
        private async Task<BasicSelectionGroupWithAssignedUsers> WithAssignedUsersAsync(BasicSelectionGroup group)
        {
            BasicSelectionGroupWithAssignedUsers groupWith = new BasicSelectionGroupWithAssignedUsers
            {
                Id = group.Id,
                RootContentItemId = group.RootContentItemId,
                IsSuspended = group.IsSuspended,
                IsInactive = group.IsInactive,
                IsMaster = group.IsMaster,
                Name = group.Name,
                AssignedUsers = (await _dbContext.UserInSelectionGroup
                                                .Where(u => u.SelectionGroupId == group.Id)
                                                .Select(u => u.UserId)
                                                .ToListAsync())
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
        private async Task<List<BasicSelectionGroupWithAssignedUsers>> WithAssignedUsersAsync(List<BasicSelectionGroup> groups)
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

                groupWith.AssignedUsers = await _dbContext.UserInSelectionGroup
                                                          .Where(u => u.SelectionGroupId == group.Id)
                                                          .Select(u => u.UserId)
                                                          .Distinct()
                                                          .ToListAsync();

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
        internal async Task<List<BasicSelectionGroup>> SelectSelectionGroupsWhereContentItemAsync(Guid contentItemId)
        {
            var selectionGroups = await _dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == contentItemId)
                .Include(g => g.RootContentItem)
                    .ThenInclude(rci => rci.ContentType)
                .OrderBy(g => g.GroupName)
                .Select(g => new BasicSelectionGroup
                {
                    Id = g.Id,
                    RootContentItemId = g.RootContentItemId,
                    IsSuspended = g.IsSuspended,
                    IsInactive = string.IsNullOrWhiteSpace(g.ContentInstanceUrl),
                    IsMaster = g.IsMaster,
                    Name = g.GroupName,
                    IsEditableEligible = g.IsEditablePowerBiEligible,
                    Editable = g.Editable,
                })
                .ToListAsync();

            return selectionGroups;
        }

        /// <summary>
        /// Select all selection groups with assigned users for a content item
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>List of selection groups with assigned users</returns>
        internal async Task<List<BasicSelectionGroupWithAssignedUsers>> SelectSelectionGroupsWithAssignedUsersAsync(Guid contentItemId)
        {
            var selectionGroups = await SelectSelectionGroupsWhereContentItemAsync(contentItemId);
            var selectionGroupsWithAssignedUsers = await WithAssignedUsersAsync(selectionGroups);

            return selectionGroupsWithAssignedUsers;
        }

        /// <summary>
        /// Select a selection group by ID with assigned users
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of selection groups with assigned users</returns>
        internal async Task<BasicSelectionGroupWithAssignedUsers> SelectSelectionGroupWithAssignedUsersAsync(Guid selectionGroupId)
        {
            var selectionGroup = await FindSelectionGroupAsync(selectionGroupId);
            var selectionGroupsWithAssignedUser = await WithAssignedUsersAsync(selectionGroup);

            return selectionGroupsWithAssignedUser;
        }

        /// <summary>
        /// Select a list of selections for a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of selections</returns>
        internal async Task<List<Guid>> SelectSelectionsWhereSelectionGroupAsync(Guid selectionGroupId)
        {
            var selections = await _dbContext.SelectionGroup
                                             .Where(g => g.Id == selectionGroupId)
                                             .Select(g => g.SelectedHierarchyFieldValueList)
                                             .SingleOrDefaultAsync();

            return selections ?? new List<Guid>();
        }

        /// <summary>
        /// Select a list of selections for multiple selection groups
        /// </summary>
        /// <param name="selectionGroupIds">List of selection group IDs</param>
        /// <returns>Dictionary of selection lists</returns>
        internal async Task<Dictionary<Guid, List<Guid>>> SelectSelectionsWhereSelectionGroupInAsync(List<Guid> selectionGroupIds)
        {
            var selectionsDict = await _dbContext.SelectionGroup
                .Where(g => selectionGroupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.SelectedHierarchyFieldValueList?.ToList());

            return selectionsDict;
        }

        /// <summary>
        /// Create a reducing selection group
        /// </summary>
        /// <param name="contentItemId">Content item ID under which the new selection group will be created</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>New selection group</returns>
        internal async Task<SelectionGroup> CreateReducingSelectionGroupAsync(Guid contentItemId, string name)
        {
            var group = new SelectionGroup
            {
                RootContentItemId = contentItemId,
                GroupName = name,
                ContentInstanceUrl = null,
                SelectedHierarchyFieldValueList = new List<Guid>(),
                IsMaster = false,
            };
            _dbContext.SelectionGroup.Add(group);

            await _dbContext.SaveChangesAsync();

            ApplicationUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group, group.RootContentItem, group.RootContentItem.Client), currentUser.UserName, currentUser.Id);

            return group;
        }

        /// <summary>
        /// Create a master selection group
        /// </summary>
        /// <param name="contentItemId">Content item ID under which the new selection group will be created</param>
        /// <param name="name">Name of the new selection group</param>
        /// <returns>New selection group</returns>
        internal async Task<SelectionGroup> CreateMasterSelectionGroupAsync(Guid contentItemId, string name)
        {
            var contentItem = await _dbContext.RootContentItem
                                              .Include(c => c.ContentType)
                                              .Include(c => c.Client)
                                              .SingleAsync(t => t.Id == contentItemId);

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
                SelectedHierarchyFieldValueList = new List<Guid>(),
                IsMaster = true,
            };
            group.SetContentUrl(contentFileName);

            _dbContext.SelectionGroup.Add(group);
            await _dbContext.SaveChangesAsync();

            ApplicationUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            _auditLogger.Log(AuditEventType.SelectionGroupCreated.ToEvent(group, contentItem, contentItem.Client), currentUser.UserName, currentUser.Id);

            return group;
        }

        /// <summary>
        /// Update the name of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="name">Selection group name</param>
        /// <returns>Selection group</returns>
        internal async Task<SelectionGroup> UpdateSelectionGroupNameAsync(Guid selectionGroupId, string name)
        {
            var group = await _dbContext.SelectionGroup.FindAsync(selectionGroupId);

            if (group.GroupName != name)
            {
                group.GroupName = name;
                await _dbContext.SaveChangesAsync();
            }

            return group;
        }

        /// <summary>
        /// Update the assigned users of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="newListOfUserIds">Selection group users</param>
        /// <returns>Selection group</returns>
        internal async Task<SelectionGroup> UpdateSelectionGroupUsersAsync(Guid selectionGroupId, List<Guid> newListOfUserIds)
        {
            var requestedGroup = await _dbContext.SelectionGroup
                .Include(g => g.RootContentItem)
                    .ThenInclude(c => c.Client)
                .SingleOrDefaultAsync(g => g.Id == selectionGroupId);

            List<ApplicationUser> allCurrentUsers = await _dbContext.UserInSelectionGroup
                .Where(uisg => uisg.SelectionGroupId == selectionGroupId)
                .Select(uisg => uisg.User)
                .ToListAsync();

            List<ApplicationUser> updatedUserList = await _dbContext.ApplicationUser
                .Where(u => newListOfUserIds.Contains(u.Id))
                .ToListAsync();

            List<ApplicationUser> usersToRemove = allCurrentUsers.Except(updatedUserList).ToList();

            List<UserInSelectionGroup> recordsToAdd = updatedUserList.Except(allCurrentUsers)
                .Select(u => new UserInSelectionGroup { UserId = u.Id, SelectionGroupId = selectionGroupId })
                .ToList();
            _dbContext.UserInSelectionGroup.AddRange(recordsToAdd);

            List<UserInSelectionGroup> recordsToRemove = await _dbContext.UserInSelectionGroup
                .Include(usg => usg.User)
                .Include(usg => usg.SelectionGroup)
                .Where(usg => usersToRemove.Select(u => u.Id).Contains(usg.UserId))
                .Where(usg => usg.SelectionGroupId == selectionGroupId)
                .ToListAsync();
            _dbContext.UserInSelectionGroup.RemoveRange(recordsToRemove);

            await _dbContext.SaveChangesAsync();

            // audit logging
            _dbContext.AttachRange(recordsToAdd);
            ApplicationUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            foreach (var userInGroup in recordsToAdd)
            {
                await _dbContext.Entry(userInGroup)?.Reference(uig => uig.User)?.LoadAsync();  // Load `User` navigation property into EF cache for this context
                _auditLogger.Log(AuditEventType.SelectionGroupUserAssigned.ToEvent(requestedGroup, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, userInGroup.User), currentUser.UserName, currentUser.Id);
            }
            foreach (var userInGroup in usersToRemove.Distinct(new IdPropertyComparer<ApplicationUser>()))
            {
                _auditLogger.Log(AuditEventType.SelectionGroupUserRemoved.ToEvent(requestedGroup, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, userInGroup), currentUser.UserName, currentUser.Id);
            }
            if (usersToRemove.Any())
            {
                _auditLogger.Log(AuditEventType.ContentDisclaimerAcceptanceReset
                    .ToEvent(recordsToRemove, requestedGroup.RootContentItem, requestedGroup.RootContentItem.Client, ContentDisclaimerResetReason.UserRemovedFromSelectionGroup), currentUser.UserName, currentUser.Id);
            }

            return requestedGroup;
        }

        /// <summary>
        /// Update the suspended status of a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <param name="isSuspended">Suspended status</param>
        /// <returns>Selection group</returns>
        internal async Task<SelectionGroup> UpdateSelectionGroupSuspendedAsync(Guid selectionGroupId, bool isSuspended)
        {
            var group = await _dbContext.SelectionGroup
                                        .Include(sg => sg.RootContentItem)
                                        .Include(sg => sg.RootContentItem).ThenInclude(ci => ci.Client)
                                        .Include(sg => sg.RootContentItem).ThenInclude(ci => ci.ContentType)
                                        .SingleAsync(sg => sg.Id == selectionGroupId);
            group.IsSuspended = isSuspended;
            await _dbContext.SaveChangesAsync();

            ApplicationUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            _auditLogger.Log(AuditEventType.SelectionGroupSuspensionUpdate.ToEvent(group, group.RootContentItem, group.RootContentItem.Client, isSuspended), currentUser.UserName, currentUser.Id);

            return group;
        }

        /// <summary>
        /// Delete a selection group
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>Deleted selection group</returns>
        internal async Task<SelectionGroup> DeleteSelectionGroupAsync(Guid selectionGroupId)
        {
            var group = await _dbContext.SelectionGroup
                                        .Include(g => g.RootContentItem)
                                            .ThenInclude(c => c.Client)
                                        .SingleOrDefaultAsync(g => g.Id == selectionGroupId);

            if (group != null)
            {
                _dbContext.SelectionGroup.Remove(group);
                await _dbContext.SaveChangesAsync();
                ApplicationUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                _auditLogger.Log(AuditEventType.SelectionGroupDeleted.ToEvent(group, group.RootContentItem, group.RootContentItem.Client), currentUser.UserName, currentUser.Id);
            }

            return group;
        }
    }
}
