/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An DI injectable resource containing queries directly related to FileDrop controller actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.FileDropModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by content access admin actions
    /// </summary>
    public class FileDropQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly HierarchyQueries _hierarchyQueries;
        private readonly UserQueries _userQueries;
        private readonly AuditLogger _auditLog;

        public FileDropQueries(
            ApplicationDbContext dbContextArg,
            ClientQueries clientQueries,
            HierarchyQueries hierarchyQueries,
            UserQueries userQueries,
            AuditLogger auditLog)
        {
            _dbContext = dbContextArg;
            _clientQueries = clientQueries;
            _hierarchyQueries = hierarchyQueries;
            _userQueries = userQueries;
            _auditLog = auditLog;
        }

        /// <summary>
        /// Return a model representing all clients that the current user should see in the FileDrop view clients column
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        internal Dictionary<Guid, ClientCardModel> GetAuthorizedClientsModel(ApplicationUser user)
        {
            List<Client> clientsWithRole = _dbContext.UserRoleInClient
                                                     .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == RoleEnum.FileDropAdmin)
                                                     .Select(urc => urc.Client)
                                                     .ToList()  // force the first query to execute
                                                     .Union(
                                                         _dbContext.ApplicationUser
                                                                   .Where(u => u.UserName.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
                                                                   .SelectMany(u => u.SftpAccounts
                                                                                     .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                                                     .Select(a => a.FileDropUserPermissionGroup.FileDrop.Client)),
                                                         new IdPropertyComparer<Client>()
                                                     )
                                                     .ToList();
            List<Guid> clientIds = clientsWithRole.ConvertAll(c => c.Id);
            List<Guid> parentClientIds = clientsWithRole
                                            .Where(c => c.ParentClientId.HasValue)
                                            .ToList()
                                            .ConvertAll(c => c.ParentClientId.Value);

            var unlistedParentClients = _dbContext.Client
                                                  .Where(c => parentClientIds.Contains(c.Id))
                                                  .Where(c => !clientIds.Contains(c.Id))
                                                  .ToList();
            List<Guid> unlistedParentClientIds = unlistedParentClients.ConvertAll(c => c.Id);

            List<ClientCardModel> returnList = new List<ClientCardModel>();
            foreach (Client eachClient in clientsWithRole)
            {
                ClientCardModel eachClientCardModel = GetClientCardModel(eachClient, user);

                // Only include information about a client's otherwise unlisted parent in the model if the user can manage the (child) client
                if (eachClientCardModel.ParentId.HasValue && 
                    unlistedParentClientIds.Contains(eachClientCardModel.ParentId.Value))
                {
                    if (eachClientCardModel.CanManageFileDrops)
                    {
                        ClientCardModel parentCardModel = GetClientCardModel(unlistedParentClients.Single(p => p.Id == eachClientCardModel.ParentId.Value), user);
                        returnList.Add(parentCardModel);
                    }
                    else
                    {
                        eachClientCardModel.ParentId = null;
                    }
                }

                returnList.Add(eachClientCardModel);
            }

            return returnList.ToDictionary(c => c.Id);
        }

        private ClientCardModel GetClientCardModel(Client client, ApplicationUser user)
        {
            return new ClientCardModel(client)
            {
                UserCount = _dbContext.ApplicationUser
                                      .Where(u => u.SftpAccounts
                                                   .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                   .Any(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id))
                                      .ToList()
                                      .Distinct(new IdPropertyComparer<ApplicationUser>())
                                      .Count(),

                FileDropCount = _dbContext.FileDrop
                                          .Count(d => d.ClientId == client.Id),

                CanManageFileDrops = _dbContext.UserRoleInClient
                                               .Any(ur => ur.ClientId == client.Id &&
                                                          ur.UserId == user.Id &&
                                                          ur.Role.RoleEnum == RoleEnum.FileDropAdmin),

                AuthorizedFileDropUser = _dbContext.SftpAccount
                                                   .Any(a => a.ApplicationUserId == user.Id &&
                                                             a.FileDropUserPermissionGroupId.HasValue &&
                                                             a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id),
            };
        }

        internal FileDropsModel GetFileDropsModelForClient(Guid clientId, ApplicationUser user)
        {
            bool userIsAdmin = _dbContext.UserRoleInClient
                                         .Any(urc => urc.UserId == user.Id &&
                                                     urc.Role.RoleEnum == RoleEnum.FileDropAdmin &&
                                                     urc.ClientId == clientId);

            List<FileDrop> fileDrops = userIsAdmin
                                       ? _dbContext.FileDrop
                                                   .Where(d => d.ClientId == clientId)
                                                   .ToList()
                                       : _dbContext.SftpAccount
                                                   .Where(a => a.ApplicationUser.Id == user.Id)
                                                   //.Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                   .Where(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == clientId)
                                                   .Select(a => a.FileDropUserPermissionGroup.FileDrop)
                                                   .ToList()
                                                   .Distinct(new IdPropertyComparer<FileDrop>())
                                                   .ToList();

            Client client = _dbContext.Client.Find(clientId);
            FileDropsModel FileDropsModel = new FileDropsModel
            {
                ClientCard = GetClientCardModel(client, user),
            };

            foreach (FileDrop eachDrop in fileDrops)
            {
                FileDropsModel.FileDrops.Add(eachDrop.Id, new FileDropCardModel(eachDrop)
                {
                    UserCount = userIsAdmin
                                ? _dbContext.SftpAccount
                                            .Where(a => a.ApplicationUserId.HasValue)
                                            .Where(a => a.FileDropUserPermissionGroup.FileDropId == eachDrop.Id)
                                            .Select(a => a.ApplicationUserId.Value)
                                            .Distinct()
                                            .Count()
                                : (int?)null,
                });
            }

            return FileDropsModel;
        }

        internal PermissionGroupsModel GetPermissionGroupsModelForFileDrop(Guid FileDropId, Guid ClientId)
        {
            var returnModel = new PermissionGroupsModel
            {
                FileDropId = FileDropId,
                EligibleUsers = _dbContext.UserRoleInClient
                                          .Include(urc => urc.User)
                                          .Where(urc => urc.ClientId == ClientId)
                                          .Where(urc => urc.Role.RoleEnum == RoleEnum.FileDropUser)
                                          .ToList()
                                          .Select(urc => new EligibleUserModel
                                              {
                                                  Id = urc.User.Id,
                                                  UserName = urc.User.UserName,
                                                  FirstName = urc.User.FirstName,
                                                  LastName = urc.User.LastName,
                                                  IsAdmin = _dbContext.UserRoleInClient
                                                                      .Any(rc => rc.UserId == urc.UserId 
                                                                              && rc.ClientId == urc.ClientId 
                                                                              && rc.Role.RoleEnum == RoleEnum.FileDropAdmin),
                                              })
                                          .ToDictionary(m => m.Id),

                PermissionGroups = _dbContext.FileDropUserPermissionGroup
                                             .Where(g => g.FileDropId == FileDropId)
                                             .ToList()
                                             .Select(g =>
                                             {
                                                 var accounts = _dbContext.SftpAccount
                                                                          .Where(a => a.FileDropUserPermissionGroupId == g.Id)
                                                                          .ToList();

                                                 return new PermissionGroupModel
                                                 {
                                                     Id = g.Id,
                                                     Name = g.Name,
                                                     IsPersonalGroup = g.IsPersonalGroup,
                                                     ReadAccess = g.ReadAccess,
                                                     WriteAccess = g.WriteAccess,
                                                     DeleteAccess = g.DeleteAccess,
                                                     AssignedSftpAccountIds = accounts.Where(a => !a.ApplicationUserId.HasValue).Select(a => a.Id).ToList(),
                                                     AssignedMapUserIds = accounts.Where(a => a.ApplicationUserId.HasValue).Select(a => a.ApplicationUserId.Value).ToList(),
                                                 };
                                             })
                                             .ToDictionary(m => m.Id),

            };

            return returnModel;
        }

        internal async Task<PermissionGroupsModel> UpdatePermissionGroups(UpdatePermissionGroupsModel model)
        {
            // audit logs to record after the database transaction succeeds
            List<Action> auditLogActions = new List<Action>();

            using (var txn = await _dbContext.Database.BeginTransactionAsync())
            {
                FileDrop fileDrop = await _dbContext.FileDrop
                                                    .Include(d => d.Client)
                                                    .SingleOrDefaultAsync(fd => fd.Id == model.FileDropId);

                #region Preliminary validation
                if (fileDrop == null)
                {
                    throw new ApplicationException("The requested FileDrop was not found");
                }
                #endregion

                List<FileDropUserPermissionGroup> groupsToRemove = await _dbContext.FileDropUserPermissionGroup
                                                                                   .Include(g => g.SftpAccounts)
                                                                                   .Where(g => model.RemovedPermissionGroupIds.Contains(g.Id))
                                                                                   .ToListAsync();

                List<FileDropUserPermissionGroup> groupsToUpdate = await _dbContext.FileDropUserPermissionGroup
                                                                                   .Include(g => g.SftpAccounts)
                                                                                   .Where(g => model.UpdatedPermissionGroups.Keys.Contains(g.Id))
                                                                                   .ToListAsync();

                List<Guid> sftpAccountIdsWithExistingAuthorization = await _dbContext.SftpAccount
                                                                                     .Where(a => a.FileDropUserPermissionGroup.FileDropId == model.FileDropId)
                                                                                     .Select(a => a.Id)
                                                                                     .ToListAsync();

                List<ApplicationUser> usersRemovedInUpdates = await _dbContext.ApplicationUser
                                                                              .Include(u => u.SftpAccounts)
                                                                              .Where(u => model.UpdatedPermissionGroups.SelectMany(g => g.Value.UsersRemoved).Contains(u.Id))
                                                                              .ToListAsync();

                List<Guid> sftpAccountIdsOfUsersRemovedInUpdates = usersRemovedInUpdates.SelectMany(u => u.SftpAccounts.Where(a => model.UpdatedPermissionGroups.Keys.Contains(a.FileDropUserPermissionGroupId.Value)))
                                                                                        .Select(a => a.Id)
                                                                                        .ToList();

                List<Guid> sftpAccountIdsWithContinuingAuthorization = sftpAccountIdsWithExistingAuthorization
                                                                                .Except(groupsToRemove.SelectMany(g => g.SftpAccounts.Select(a => a.Id)))
                                                                                .Except(sftpAccountIdsOfUsersRemovedInUpdates)
                                                                                .ToList();

                #region model validation
                if (groupsToRemove.Any(g => g.FileDropId != model.FileDropId))
                {
                    throw new ApplicationException("A permission group requested for removal is not associated with this file drop");
                }
                if (groupsToRemove.Count != model.RemovedPermissionGroupIds.Count)
                {
                    throw new ApplicationException("One or more permission groups requested for removal was not found");
                }
                if (groupsToUpdate.Any(g => g.FileDropId != model.FileDropId))
                {
                    throw new ApplicationException("A permission group requested for update is not associated with this file drop");
                }
                if (groupsToUpdate.Count != model.UpdatedPermissionGroups.Keys.Count)
                {
                    throw new ApplicationException("One or more permission groups requested for update was not found");
                }
                if (sftpAccountIdsOfUsersRemovedInUpdates.Count != model.UpdatedPermissionGroups.SelectMany(u => u.Value.UsersRemoved).Count())
                {
                    throw new ApplicationException("One or more SFTP accounts requested for removal from permission group(s) was not found");
                }
                #endregion

                // Handle removed groups.  This unassigns accounts so they can be reassigned by leveraging ON DELETE SET NULL of the FK relationship
                foreach (var groupToRemove in groupsToRemove)
                {
                    _dbContext.FileDropUserPermissionGroup.RemoveRange(groupsToRemove);
                    auditLogActions.Add(() => _auditLog.Log(AuditEventType.FileDropPermissionGroupDeleted.ToEvent(fileDrop, groupToRemove, fileDrop.Client.Id, fileDrop.Client.Name)));
                }

                foreach (var updatedGroupRecord in groupsToUpdate)
                {
                    UpdatedPermissionGroup modelForUpdatedGroup = model.UpdatedPermissionGroups[updatedGroupRecord.Id];

                    // Update group properties
                    updatedGroupRecord.Name = modelForUpdatedGroup.Name;
                    updatedGroupRecord.ReadAccess = modelForUpdatedGroup.ReadAccess;
                    updatedGroupRecord.WriteAccess = modelForUpdatedGroup.WriteAccess;
                    updatedGroupRecord.DeleteAccess = modelForUpdatedGroup.DeleteAccess;

                    // Unassign accounts of users who are being removed from existing groups
                    List<SftpAccount> userAccountsToRemove = updatedGroupRecord.SftpAccounts
                                                                     .Where(a => modelForUpdatedGroup.UsersRemoved.Contains(a.ApplicationUserId.Value))
                                                                     .ToList();
                    foreach (SftpAccount removedAccount in userAccountsToRemove)
                    {
                        updatedGroupRecord.SftpAccounts.Remove(removedAccount);
                    }

                    // Remove non-user accounts
                    List<SftpAccount> nonUserAccountsToRemove = updatedGroupRecord.SftpAccounts
                                                                     .Where(a => modelForUpdatedGroup.SftpAccountsRemoved.Contains(a.Id))
                                                                     .ToList();
                    foreach (SftpAccount removedAccount in nonUserAccountsToRemove)
                    {
                        updatedGroupRecord.SftpAccounts.Remove(removedAccount);
                    }
                }
                _dbContext.SaveChanges();

                foreach (var updatedGroupRecord in groupsToUpdate)
                {
                    List<Guid> userIdList = model.UpdatedPermissionGroups[updatedGroupRecord.Id].UsersAdded;

                    List<SftpAccount> existinguserAccountsToAdd = _dbContext.SftpAccount
                                                                .Where(a => userIdList.Contains(a.ApplicationUserId.Value))
                                                                .Where(a => a.FileDropId == model.FileDropId)
                                                                .ToList();

                    List<Guid> userIdsRequiringNewAccount = userIdList.Except(existinguserAccountsToAdd.Select(a => a.ApplicationUserId.Value)).ToList();
                    List<ApplicationUser> usersRequiringNewAccount = _dbContext.ApplicationUser
                                                                               .Where(u => userIdsRequiringNewAccount.Contains(u.Id))
                                                                               .ToList();

                    foreach (Guid userIdToAdd in userIdList)
                    {
                        SftpAccount accountToAdd;
                        if (userIdsRequiringNewAccount.Contains(userIdToAdd))
                        {
                            accountToAdd = new SftpAccount(model.FileDropId)
                            {
                                ApplicationUserId = userIdToAdd,
                                IsSuspended = false,
                                UserName = usersRequiringNewAccount.Single(u => u.Id == userIdToAdd).UserName
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(accountToAdd, fileDrop)));                            
                        }
                        else
                        {
                            accountToAdd = existinguserAccountsToAdd.SingleOrDefault(a => a.ApplicationUserId.Value == userIdToAdd);
                        }

                        updatedGroupRecord.SftpAccounts.Add(accountToAdd);
                    }

                    // Handle non-user Sftp accounts added to this group
                    List<Guid> existingNonUserAccountIdsToAdd = model.UpdatedPermissionGroups[updatedGroupRecord.Id]
                                                                     .SftpAccountsAdded
                                                                     .Where(a => a.Id.HasValue && a.Id.Value != Guid.Empty)
                                                                     .Select(a => a.Id.Value)
                                                                     .ToList();
                    List<SftpAccount> existingNonUserAccountsToAdd = _dbContext.SftpAccount
                                                                               .Where(a => existingNonUserAccountIdsToAdd.Contains(a.Id))
                                                                               .ToList();

                    foreach (var accountToAdd in model.UpdatedPermissionGroups[updatedGroupRecord.Id].SftpAccountsAdded)
                    {
                        SftpAccount sftpAccountToAdd = default;
                        if (accountToAdd.Id.HasValue)
                        {
                            sftpAccountToAdd = existingNonUserAccountsToAdd.Single(a => a.Id == accountToAdd.Id.Value);
                        }
                        else
                        {
                            sftpAccountToAdd = new SftpAccount(model.FileDropId)
                            {
                                IsSuspended = accountToAdd.IsSuspended,
                                UserName = accountToAdd.AccountName,
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(sftpAccountToAdd, fileDrop)));
                        }
                        updatedGroupRecord.SftpAccounts.Add(sftpAccountToAdd);
                    }
                }
                _dbContext.SaveChanges();

                foreach (NewPermissionGroup newGroup in model.NewPermissionGroups)
                {
                    FileDropUserPermissionGroup newFileDropUserPermissionGroup = new FileDropUserPermissionGroup
                    {
                        Name = newGroup.Name,
                        ReadAccess = newGroup.ReadAccess,
                        WriteAccess = newGroup.WriteAccess,
                        DeleteAccess = newGroup.DeleteAccess,
                        FileDropId = model.FileDropId,
                        IsPersonalGroup = newGroup.IsPersonalGroup,
                    };

                    List<SftpAccount> existingSftpAccountsOfGroupUsers = _dbContext.SftpAccount
                                                                                   .Include(a => a.ApplicationUser)
                                                                                   //.Where(a => a.ApplicationUserId.HasValue)
                                                                                   .Where(a => newGroup.AssignedMapUserIds.Contains(a.ApplicationUserId.Value))
                                                                                   .Where(a => a.FileDropId == model.FileDropId)
                                                                                   .ToList();

                    foreach (Guid userId in newGroup.AssignedMapUserIds)
                    {
                        SftpAccount userSftpAccount = existingSftpAccountsOfGroupUsers.SingleOrDefault(a => a.ApplicationUserId == userId);
                        if (userSftpAccount == null)
                        {
                            userSftpAccount = new SftpAccount(fileDrop.Id)
                            {
                                ApplicationUserId = userId,
                                IsSuspended = false,
                                UserName = _dbContext.ApplicationUser.Find(userId).UserName,
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(userSftpAccount, fileDrop)));
                        }

                        newFileDropUserPermissionGroup.SftpAccounts.Add(userSftpAccount);
                    }

                    foreach (NonUserSftpAccount newAccount in newGroup.AssignedSftpAccounts)
                    {
                        SftpAccount newSftpAccount = new SftpAccount(model.FileDropId)
                        {
                            ApplicationUserId = null,
                            IsSuspended = newAccount.IsSuspended,
                            UserName = newAccount.AccountName,
                        };
                        newFileDropUserPermissionGroup.SftpAccounts.Add(newSftpAccount);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(newSftpAccount, fileDrop)));
                    }

                    _dbContext.FileDropUserPermissionGroup.Add(newFileDropUserPermissionGroup);
                    auditLogActions.Add(() => _auditLog.Log(AuditEventType.FileDropPermissionGroupCreated.ToEvent(fileDrop, newFileDropUserPermissionGroup, fileDrop.Client.Id, fileDrop.Client.Name)));
                }

                _dbContext.SaveChanges();

                txn.Commit();

                foreach (var logAction in auditLogActions)
                {
                    logAction();
                }

                return GetPermissionGroupsModelForFileDrop(model.FileDropId, fileDrop.ClientId);
            }
        }
    }
}
