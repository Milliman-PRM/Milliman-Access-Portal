/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An DI injectable resource containing queries directly related to FileDrop controller actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.FileDropModels;
using MillimanAccessPortal.Services;
using nsoftware.IPWorksSSH;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly IConfiguration _appConfig;
        private readonly IAuditLogger _auditLog;
        private readonly IFileDropUploadTaskTracker _fileDropUploadTaskTracker;
        private readonly IServiceProvider _serviceProvider;

    public FileDropQueries(
            ApplicationDbContext dbContextArg,
            ClientQueries clientQueries,
            HierarchyQueries hierarchyQueries,
            UserQueries userQueries,
            IConfiguration configuration,
            IAuditLogger auditLog,
            IFileDropUploadTaskTracker fileDropUploadTaskTrackerArg,
            IServiceProvider serviceProvider
            )
        {
            _dbContext = dbContextArg;
            _clientQueries = clientQueries;
            _hierarchyQueries = hierarchyQueries;
            _userQueries = userQueries;
            _appConfig = configuration;
            _auditLog = auditLog;
            _fileDropUploadTaskTracker = fileDropUploadTaskTrackerArg;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Return a model representing all clients that the current user should see in the FileDrop view clients column
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        internal async Task<Dictionary<Guid, ClientCardModel>> GetAuthorizedClientsModelAsync(ApplicationUser user)
        {
            List<Client> clientsWithRole = (await _dbContext.UserRoleInClient
                                                     .Where(urc => urc.UserId == user.Id 
                                                                && urc.Role.RoleEnum == RoleEnum.FileDropAdmin)
                                                     .Select(urc => urc.Client)
                                                     .ToListAsync())  // force the first query to execute
                                                     .Union(
                                                         _dbContext.UserRoleInClient
                                                                   .Where(urc => urc.UserId == user.Id 
                                                                              && urc.Role.RoleEnum == RoleEnum.FileDropUser)
                                                                   .SelectMany(urc => urc.User.SftpAccounts
                                                                                              .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                                                              .Where(a => a.FileDropUserPermissionGroup.ReadAccess
                                                                                                       || a.FileDropUserPermissionGroup.WriteAccess
                                                                                                       || a.FileDropUserPermissionGroup.DeleteAccess)
                                                                                              .Select(a => a.FileDrop.Client)),
                                                         new IdPropertyComparer<Client>()
                                                     )
                                                     .ToList()
                                                     .FindAll(c => DateTime.UtcNow.Date - c.LastAccessReview.LastReviewDateTimeUtc.Date <= TimeSpan.FromDays(_appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")));
            List<Guid> clientIds = clientsWithRole.ConvertAll(c => c.Id);
            List<Guid> parentClientIds = clientsWithRole
                                            .Where(c => c.ParentClientId.HasValue)
                                            .ToList()
                                            .ConvertAll(c => c.ParentClientId.Value);

            var unlistedParentClients = await _dbContext.Client
                                                        .Where(c => parentClientIds.Contains(c.Id))
                                                        .Where(c => !clientIds.Contains(c.Id))
                                                        .ToListAsync();
            List<Guid> unlistedParentClientIds = unlistedParentClients.ConvertAll(c => c.Id);

            List<ClientCardModel> returnList = new List<ClientCardModel>();
            foreach (Client eachClient in clientsWithRole)
            {
                ClientCardModel eachClientCardModel = await GetClientCardModelAsync(eachClient, user.Id);

                // Only include information about a client's otherwise unlisted parent in the model if the user can manage the (child) client
                if (eachClientCardModel.ParentId.HasValue && 
                    unlistedParentClientIds.Contains(eachClientCardModel.ParentId.Value))
                {
                    if (eachClientCardModel.CanManageFileDrops)
                    {
                        ClientCardModel parentCardModel = await GetClientCardModelAsync(unlistedParentClients.Single(p => p.Id == eachClientCardModel.ParentId.Value), user.Id);
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

        private async Task<ClientCardModel> GetClientCardModelAsync(Client client, Guid userId)
        {
            bool userIsAdmin = await _dbContext.UserRoleInClient
                                               .AnyAsync(urc => urc.UserId == userId &&
                                                                urc.Role.RoleEnum == RoleEnum.FileDropAdmin &&
                                                                urc.ClientId == client.Id);

            return new ClientCardModel(client)
            {
                UserCount = await _dbContext.ApplicationUser
                                            .Where(u => u.SftpAccounts
                                                         .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                         .Where(a => a.FileDropUserPermissionGroup.ReadAccess
                                                                  || a.FileDropUserPermissionGroup.WriteAccess
                                                                  || a.FileDropUserPermissionGroup.DeleteAccess)
                                                         .Any(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id))
                                            .Select(u => u.Id)
                                            .Distinct()
                                            .CountAsync(),

                FileDropCount = userIsAdmin
                                       ? await _dbContext.FileDrop
                                                         .Where(d => d.ClientId == client.Id)
                                                         .CountAsync()
                                       : await _dbContext.SftpAccount
                                                         .Where(a => a.ApplicationUser.Id == userId)
                                                         .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                         .Where(a => a.FileDropUserPermissionGroup.ReadAccess
                                                                  || a.FileDropUserPermissionGroup.WriteAccess
                                                                  || a.FileDropUserPermissionGroup.DeleteAccess)
                                                         .Where(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id)
                                                         .Select(a => a.FileDropUserPermissionGroup.FileDropId)
                                                         .Distinct()
                                                         .CountAsync(),

                CanManageFileDrops = await _dbContext.UserRoleInClient
                                                     .AnyAsync(ur => ur.ClientId == client.Id &&
                                                                     ur.UserId == userId &&
                                                                     ur.Role.RoleEnum == RoleEnum.FileDropAdmin),

                AuthorizedFileDropUser = await _dbContext.SftpAccount
                                                        .AnyAsync(a => a.ApplicationUserId == userId &&
                                                                       a.FileDropUserPermissionGroupId.HasValue &&
                                                                       (a.FileDropUserPermissionGroup.ReadAccess || a.FileDropUserPermissionGroup.WriteAccess || a.FileDropUserPermissionGroup.DeleteAccess) &&
                                                                       a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id),

                IsAccessReviewExpired = DateTime.UtcNow.Date - client.LastAccessReview.LastReviewDateTimeUtc.Date > TimeSpan.FromDays(_appConfig.GetValue<int>("")),
            };
        }

        internal async Task<FileDropsModel> GetFileDropsModelForClientAsync(Guid clientId, Guid userId)
        {
            List<FileDrop> fileDrops = new List<FileDrop>();

            Client client = await _dbContext.Client.FindAsync(clientId);

            FileDropsModel FileDropsModel = new FileDropsModel
            {
                ClientCard = await GetClientCardModelAsync(client, userId),
            };

            bool userIsAdmin = await _dbContext.UserRoleInClient
                                               .AnyAsync(urc => urc.UserId == userId &&
                                                                urc.Role.RoleEnum == RoleEnum.FileDropAdmin &&
                                                                urc.ClientId == clientId);

            // only include file drop details if the client access review deadline is not expired
            if (DateTime.UtcNow.Date - client.LastAccessReview.LastReviewDateTimeUtc.Date <= TimeSpan.FromDays(_appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")))
            {
                fileDrops = userIsAdmin
                            ? await _dbContext.FileDrop
                                              .Where(d => d.ClientId == clientId)
                                              .ToListAsync()
                            : (await _dbContext.SftpAccount
                                               .Where(a => a.ApplicationUser.Id == userId)
                                               .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                               .Where(a => a.FileDropUserPermissionGroup.ReadAccess
                                                        || a.FileDropUserPermissionGroup.WriteAccess
                                                        || a.FileDropUserPermissionGroup.DeleteAccess)
                                               .Where(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == clientId)
                                               .Select(a => a.FileDropUserPermissionGroup.FileDrop)
                                               .ToListAsync())
                                     .Distinct(new IdPropertyComparer<FileDrop>())
                                     .ToList();
            }

            foreach (FileDrop eachDrop in fileDrops)
            {
                FileDropsModel.FileDrops.Add(eachDrop.Id, new FileDropCardModel(eachDrop)
                {
                    UserCount = userIsAdmin
                                ? await _dbContext.SftpAccount
                                                  .Where(a => a.ApplicationUserId.HasValue)
                                                  .Where(a => a.FileDropId == eachDrop.Id)
                                                  .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                  .Where(a => a.FileDropUserPermissionGroup.ReadAccess
                                                           || a.FileDropUserPermissionGroup.WriteAccess
                                                           || a.FileDropUserPermissionGroup.DeleteAccess)
                                                  .Select(a => a.ApplicationUserId.Value)
                                                  .Distinct()
                                                  .CountAsync()
                                : (int?)null,
                    CurrentUserPermissions = await _dbContext.SftpAccount
                                                             .Where(a => a.FileDropId == eachDrop.Id)
                                                             .Where(a => a.ApplicationUserId == userId)
                                                             .Select(a => new PermissionSet
                                                             {
                                                                 ReadAccess = a.FileDropUserPermissionGroup.ReadAccess,
                                                                 WriteAccess = a.FileDropUserPermissionGroup.WriteAccess,
                                                                 DeleteAccess = a.FileDropUserPermissionGroup.DeleteAccess,
                                                             })
                                                             .SingleOrDefaultAsync(),
                });
            }

            return FileDropsModel;
        }

        internal async Task<PermissionGroupsModel> GetPermissionGroupsModelForFileDropAsync(Guid FileDropId, Guid ClientId, ApplicationUser currentUser)
        {
            var returnModel = new PermissionGroupsModel
            {
                FileDropId = FileDropId,
                EligibleUsers = (await _dbContext.UserRoleInClient
                                          .Include(urc => urc.User)
                                          .Where(urc => urc.ClientId == ClientId)
                                          .Where(urc => urc.Role.RoleEnum == RoleEnum.FileDropUser)
                                          .ToListAsync())
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

                PermissionGroups = (await _dbContext.FileDropUserPermissionGroup
                                             .Where(g => g.FileDropId == FileDropId)
                                             .ToListAsync())
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
                                                Permissions = new PermissionSet
                                                {
                                                    ReadAccess = g.ReadAccess,
                                                    WriteAccess = g.WriteAccess,
                                                    DeleteAccess = g.DeleteAccess,
                                                },
                                                AssignedSftpAccountIds = accounts.Where(a => !a.ApplicationUserId.HasValue)
                                                                                 .Select(a => a.Id)
                                                                                 .Distinct()
                                                                                 .ToList(),
                                                AssignedMapUserIds = accounts.Where(a => a.ApplicationUserId.HasValue)
                                                                             .Select(a => a.ApplicationUserId.Value)
                                                                             .Distinct()
                                                                             .ToList(),
                                            };
                                        })
                                        .ToDictionary(m => m.Id),
                ClientModel = await GetClientCardModelAsync(await _dbContext.Client.SingleOrDefaultAsync(c => c.Id == ClientId), currentUser.Id),
            };

            return returnModel;
        }

        internal async Task<PermissionGroupsModel> UpdatePermissionGroupsAsync(UpdatePermissionGroupsModel model, ApplicationUser currentUser)
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
                                                                                       .ThenInclude(a => a.ApplicationUser)
                                                                                   .Where(g => model.RemovedPermissionGroupIds.Contains(g.Id))
                                                                                   .ToListAsync();

                List<FileDropUserPermissionGroup> groupsToUpdate = await _dbContext.FileDropUserPermissionGroup
                                                                                   .Include(g => g.SftpAccounts)
                                                                                       .ThenInclude(a => a.ApplicationUser)
                                                                                   .Where(g => model.UpdatedPermissionGroups.Keys.Contains(g.Id))
                                                                                   .ToListAsync();

                List<Guid> sftpAccountIdsWithExistingAuthorization = await _dbContext.SftpAccount
                                                                                     .Include(a => a.ApplicationUser)
                                                                                     .Where(a => a.FileDropUserPermissionGroup.FileDropId == model.FileDropId)
                                                                                     .Select(a => a.Id)
                                                                                     .ToListAsync();
                
                Guid[] userIdsRemovedInUpdatedGroups = model.UpdatedPermissionGroups.SelectMany(g => g.Value.UsersRemoved).ToArray();
                List<ApplicationUser> usersRemovedInUpdates = await _dbContext.ApplicationUser
                                                                              .Include(u => u.SftpAccounts)
                                                                                  .ThenInclude(a => a.ApplicationUser)
                                                                              .Where(u => userIdsRemovedInUpdatedGroups.Contains(u.Id))
                                                                              .ToListAsync();

                List<Guid> sftpAccountIdsOfUsersRemovedInUpdates = usersRemovedInUpdates.SelectMany(u => u.SftpAccounts.Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                                                                                       .Where(a => model.UpdatedPermissionGroups.Keys.Contains(a.FileDropUserPermissionGroupId.Value)))
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
                    foreach (var account in groupToRemove.SftpAccounts)
                    {
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(account, groupToRemove, fileDrop)));
                    }
                    _dbContext.FileDropUserPermissionGroup.RemoveRange(groupsToRemove);
                    auditLogActions.Add(() => _auditLog.Log(AuditEventType.FileDropPermissionGroupDeleted.ToEvent(fileDrop, groupToRemove)));
                }

                foreach (var updatedGroupRecord in groupsToUpdate)
                {
                    UpdatedPermissionGroup modelForUpdatedGroup = model.UpdatedPermissionGroups[updatedGroupRecord.Id];

                    // Update group properties
                    if (updatedGroupRecord.Name != modelForUpdatedGroup.Name ||
                        updatedGroupRecord.ReadAccess != modelForUpdatedGroup.Permissions.ReadAccess ||
                        updatedGroupRecord.WriteAccess != modelForUpdatedGroup.Permissions.WriteAccess ||
                        updatedGroupRecord.DeleteAccess != modelForUpdatedGroup.Permissions.DeleteAccess)
                    {
                        var previousGroup = new FileDropUserPermissionGroup
                        {
                            Id = updatedGroupRecord.Id,
                            IsPersonalGroup = updatedGroupRecord.IsPersonalGroup,
                            Name = updatedGroupRecord.Name,
                            ReadAccess = updatedGroupRecord.ReadAccess,
                            WriteAccess = updatedGroupRecord.WriteAccess,
                            DeleteAccess = updatedGroupRecord.DeleteAccess,
                        };
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.PermissionGroupUpdated.ToEvent(previousGroup, (FileDropPermissionGroupLogModel)modelForUpdatedGroup, fileDrop)));
                        updatedGroupRecord.Name = modelForUpdatedGroup.Name;
                        updatedGroupRecord.ReadAccess = modelForUpdatedGroup.Permissions.ReadAccess;
                        updatedGroupRecord.WriteAccess = modelForUpdatedGroup.Permissions.WriteAccess;
                        updatedGroupRecord.DeleteAccess = modelForUpdatedGroup.Permissions.DeleteAccess;
                    }

                    // Unassign accounts of users who are being removed from existing groups
                    List<SftpAccount> userAccountsToRemove = updatedGroupRecord.SftpAccounts
                                                                     .Where(a => modelForUpdatedGroup.UsersRemoved.Contains(a.ApplicationUserId.Value))
                                                                     .ToList();
                    foreach (SftpAccount removedAccount in userAccountsToRemove)
                    {
                        updatedGroupRecord.SftpAccounts.Remove(removedAccount);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(removedAccount, updatedGroupRecord, fileDrop)));
                    }

                    // Remove non-user accounts
                    List<SftpAccount> nonUserAccountsToRemove = updatedGroupRecord.SftpAccounts
                                                                     .Where(a => modelForUpdatedGroup.SftpAccountsRemoved.Contains(a.Id))
                                                                     .ToList();
                    foreach (SftpAccount removedAccount in nonUserAccountsToRemove)
                    {
                        updatedGroupRecord.SftpAccounts.Remove(removedAccount);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountRemovedFromPermissionGroup.ToEvent(removedAccount, updatedGroupRecord, fileDrop)));
                    }
                }
                await _dbContext.SaveChangesAsync();

                foreach (var updatedGroupRecord in groupsToUpdate)
                {
                    List<Guid> userIdList = model.UpdatedPermissionGroups[updatedGroupRecord.Id].UsersAdded;

                    List<SftpAccount> existinguserAccountsToAdd = await _dbContext.SftpAccount
                                                                .Include(a => a.ApplicationUser)
                                                                .Where(a => userIdList.Contains(a.ApplicationUserId.Value))
                                                                .Where(a => a.FileDropId == model.FileDropId)
                                                                .ToListAsync();

                    List<Guid> userIdsRequiringNewAccount = userIdList.Except(existinguserAccountsToAdd.Select(a => a.ApplicationUserId.Value)).ToList();
                    List<ApplicationUser> usersRequiringNewAccount = await _dbContext.ApplicationUser
                                                                                     .Where(u => userIdsRequiringNewAccount.Contains(u.Id))
                                                                                     .ToListAsync();

                    foreach (Guid userIdToAdd in userIdList)
                    {
                        SftpAccount accountToAdd;
                        if (userIdsRequiringNewAccount.Contains(userIdToAdd))
                        {
                            accountToAdd = new SftpAccount(model.FileDropId)
                            {
                                ApplicationUser = usersRequiringNewAccount.Single(u => u.Id == userIdToAdd),
                                IsSuspended = false,
                                UserName = usersRequiringNewAccount.Single(u => u.Id == userIdToAdd).UserName + $"-{fileDrop.ShortHash}",
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(accountToAdd, fileDrop)));                            
                        }
                        else
                        {
                            accountToAdd = existinguserAccountsToAdd.SingleOrDefault(a => a.ApplicationUserId.Value == userIdToAdd);
                        }

                        updatedGroupRecord.SftpAccounts.Add(accountToAdd);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountAddedToPermissionGroup.ToEvent(accountToAdd, updatedGroupRecord, fileDrop)));
                    }

                    // Handle non-user Sftp accounts added to this group
                    List<Guid> existingNonUserAccountIdsToAdd = model.UpdatedPermissionGroups[updatedGroupRecord.Id]
                                                                     .SftpAccountsAdded
                                                                     .Where(a => a.Id.HasValue && a.Id.Value != Guid.Empty)
                                                                     .Select(a => a.Id.Value)
                                                                     .ToList();
                    List<SftpAccount> existingNonUserAccountsToAdd = await _dbContext.SftpAccount
                                                                                     .Where(a => existingNonUserAccountIdsToAdd.Contains(a.Id))
                                                                                     .ToListAsync();

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
                                UserName = accountToAdd.AccountName + $"-{fileDrop.ShortHash}",
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(sftpAccountToAdd, fileDrop)));
                        }
                        updatedGroupRecord.SftpAccounts.Add(sftpAccountToAdd);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountAddedToPermissionGroup.ToEvent(sftpAccountToAdd, updatedGroupRecord, fileDrop)));
                    }
                }
                await _dbContext.SaveChangesAsync();

                foreach (NewPermissionGroup newGroup in model.NewPermissionGroups)
                {
                    List<ApplicationUser> assignedMapUsers = await _dbContext.ApplicationUser
                                                                             .Where(u => newGroup.AssignedMapUserIds.Contains(u.Id))
                                                                             .ToListAsync();
                    if (newGroup.AssignedMapUserIds.Count != assignedMapUsers.Count)
                    {
                        await txn.RollbackAsync();
                        Log.Warning($"Users with Ids [{string.Join(",", newGroup.AssignedMapUserIds.Except(assignedMapUsers.Select(u=>u.Id)))}] not found, cannot be added in new permission group {newGroup.Name}, file drop {fileDrop.Name}, aborting");
                        throw new ApplicationException("One or more requested users not found");
                    }

                    var authorizedUserIds = await _dbContext.UserRoleInClient
                                                            .Where(urc => urc.ClientId == fileDrop.ClientId)
                                                            .Where(urc => newGroup.AssignedMapUserIds.Contains(urc.UserId))
                                                            .Where(urc => urc.Role.RoleEnum == RoleEnum.FileDropUser)
                                                            .Select(urc => urc.UserId)
                                                            .ToListAsync();

                    var unauthorizedUsers = assignedMapUsers.Where(u => !authorizedUserIds.Contains(u.Id));
                    if (unauthorizedUsers.Any())
                    {
                        await txn.RollbackAsync();
                        Log.Warning($"Users [{string.Join(",", unauthorizedUsers.Select(u => u.UserName))}] are not authorized to file drop {fileDrop.Id}, cannot be added in new permission group {newGroup.Name}, file drop {fileDrop.Name}, aborting");
                        throw new ApplicationException("One or more requested users are not authorized to this file drop");
                    }

                    FileDropUserPermissionGroup newFileDropUserPermissionGroup = new FileDropUserPermissionGroup
                    {
                        Name = newGroup.Name,
                        ReadAccess = newGroup.Permissions.ReadAccess,
                        WriteAccess = newGroup.Permissions.WriteAccess,
                        DeleteAccess = newGroup.Permissions.DeleteAccess,
                        FileDrop = fileDrop,
                        IsPersonalGroup = newGroup.IsPersonalGroup,
                    };
                    auditLogActions.Add(() => _auditLog.Log(AuditEventType.FileDropPermissionGroupCreated.ToEvent(fileDrop, newFileDropUserPermissionGroup, fileDrop.Client.Id, fileDrop.Client.Name)));

                    List<SftpAccount> existingSftpAccountsOfGroupUsers = await _dbContext.SftpAccount
                                                                                         .Include(a => a.ApplicationUser)
                                                                                         .Where(a => a.ApplicationUserId.HasValue)
                                                                                         .Where(a => newGroup.AssignedMapUserIds.Contains(a.ApplicationUserId.Value))
                                                                                         .Where(a => a.FileDropId == model.FileDropId)
                                                                                         .ToListAsync();

                    foreach (var user in assignedMapUsers)
                    {
                        SftpAccount userSftpAccount = existingSftpAccountsOfGroupUsers.SingleOrDefault(a => a.ApplicationUserId == user.Id);
                        if (userSftpAccount == null)
                        {
                            userSftpAccount = new SftpAccount(fileDrop.Id)
                            {
                                ApplicationUserId = user.Id,
                                IsSuspended = false,
                                UserName = user.UserName + $"-{fileDrop.ShortHash}",
                            };
                            auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(userSftpAccount, fileDrop)));
                        }
                        else if (userSftpAccount.FileDropUserPermissionGroupId.HasValue)
                        {
                            await txn.RollbackAsync();
                            Log.Warning($"User {user.UserName} ({user.Id}) is already assigned to file drop {fileDrop.Id}, cannot be added in new permission group {newGroup.Name}, file drop {fileDrop.Name}, aborting");
                            throw new ApplicationException("One or more requested users are already assigned to this file drop");
                        }

                        newFileDropUserPermissionGroup.SftpAccounts.Add(userSftpAccount);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountAddedToPermissionGroup.ToEvent(userSftpAccount, newFileDropUserPermissionGroup, fileDrop)));
                    }

                    foreach (NonUserSftpAccount newAccount in newGroup.AssignedSftpAccounts)
                    {
                        SftpAccount newSftpAccount = new SftpAccount(model.FileDropId)
                        {
                            ApplicationUserId = null,
                            IsSuspended = newAccount.IsSuspended,
                            UserName = newAccount.AccountName + $"-{fileDrop.ShortHash}",
                        };
                        newFileDropUserPermissionGroup.SftpAccounts.Add(newSftpAccount);
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.SftpAccountCreated.ToEvent(newSftpAccount, fileDrop)));
                        auditLogActions.Add(() => _auditLog.Log(AuditEventType.AccountAddedToPermissionGroup.ToEvent(newSftpAccount, newFileDropUserPermissionGroup, fileDrop)));
                    }

                    _dbContext.FileDropUserPermissionGroup.Add(newFileDropUserPermissionGroup);
                }

                await _dbContext.SaveChangesAsync();

                txn.Commit();

                foreach (var logAction in auditLogActions)
                {
                    logAction();
                }

                return await GetPermissionGroupsModelForFileDropAsync(model.FileDropId, fileDrop.ClientId, currentUser);
            }
        }

        internal async Task<SftpAccountSettingsModel> GetAccountSettingsModelAsync(Guid fileDropId, ApplicationUser user)
        {
            SftpAccount userSftpAccount = await _dbContext.SftpAccount
                                                          .Include(a => a.FileDropUserPermissionGroup)
                                                              .ThenInclude(g => g.FileDrop)
                                                          .Where(a => a.FileDropUserPermissionGroup.FileDropId == fileDropId)
                                                          .SingleOrDefaultAsync(a => a.ApplicationUserId == user.Id);

            string privateKeyString = _appConfig.GetValue<string>("SftpServerPrivateKey").Replace(@"\n", "\n");
            byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyString);
            Certificate certificate = new Certificate(privateKeyBytes);

            var returnModel = new SftpAccountSettingsModel
                    {
                        SftpHost = _appConfig.GetValue<string>("SftpServerHost"),
                        SftpPort = _appConfig.GetValue("SftpServerPort", SftpAccountSettingsModel.DefaultPort),
                        Fingerprint = certificate.Fingerprint,
                    };

            if (userSftpAccount != null)
            {
                int sftpPasswordExpirationDays = _appConfig.GetValue("SftpPasswordExpirationDays", 60);

                returnModel.SftpUserName = userSftpAccount.UserName;
                returnModel.UserHasPassword = !string.IsNullOrWhiteSpace(userSftpAccount.PasswordHash);
                returnModel.IsSuspended = userSftpAccount.IsSuspended;
                returnModel.IsPasswordExpired = userSftpAccount.PasswordResetDateTimeUtc < DateTime.UtcNow - TimeSpan.FromDays(sftpPasswordExpirationDays);
                returnModel.AssignedPermissionGroupId = userSftpAccount.FileDropUserPermissionGroupId;

                if (userSftpAccount.FileDropUserPermissionGroup != null)
                {
                    bool userIsFileDropAdmin = _dbContext.UserRoleInClient.Any(urc => urc.UserId == user.Id
                                                                                   && urc.ClientId == userSftpAccount.FileDropUserPermissionGroup.FileDrop.ClientId
                                                                                   && urc.Role.RoleEnum == RoleEnum.FileDropAdmin);

                    foreach (FileDropNotificationType type in Enum.GetValues(typeof(FileDropNotificationType)))
                    {
                        var dbSetting = userSftpAccount.NotificationSubscriptions.SingleOrDefault(n => n.NotificationType == type);

                        bool canModify = false;
                        switch (type)
                        {
                            case FileDropNotificationType.FileWrite:
                                canModify = userIsFileDropAdmin ||
                                            userSftpAccount.FileDropUserPermissionGroup.WriteAccess ||
                                            userSftpAccount.FileDropUserPermissionGroup.ReadAccess ||
                                            userSftpAccount.FileDropUserPermissionGroup.DeleteAccess;
                                break;

                            case FileDropNotificationType.FileRead:
                                break;

                            case FileDropNotificationType.FileDelete:
                                break;

                            default:  // Can only happen if a new enum value is added
                                string msg = $"Encountered unsupported FileDropNotificationType value <{type}> in FileDropQueries.GetAccountSettingsModelAsync";
                                Log.Error(msg);
                                throw new ApplicationException(msg);
                        }

                        returnModel.Notifications.Add(new NotificationModel { NotificationType = type, CanModify = canModify, IsEnabled = dbSetting?.IsEnabled ?? false });
                    }
                }
            }

            return returnModel;
        }

        internal async Task<UploadStatusModel> GetUploadTaskStatusAsync(Guid taskId, Guid fileDropId)
        {
            FileDropUploadTask requestedTask = _fileDropUploadTaskTracker.GetExistingTask(taskId);

            if (requestedTask == null)
            {
                Log.Information($"GetUploadTaskStatusAsync: requested task with Id {taskId} not found, returning status {FileDropUploadTaskStatus.Unknown}");
                return new UploadStatusModel { Status = FileDropUploadTaskStatus.Unknown };
            }

            FileDropDirectory directory = await _dbContext.FileDropDirectory.FindAsync(requestedTask.FileDropDirectoryId);

            if (directory.FileDropId != fileDropId)
            {
                Log.Warning($"GetUploadTaskStatusAsync: requested task with Id {taskId} does not relate to requested FileDrop {fileDropId}");
                throw new ApplicationException("An error was encountered.");  // Don't want to be too clear about this
            }

            return new UploadStatusModel { Status = requestedTask.Status, FileName = requestedTask.FileName };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileDropId"></param>
        /// <param name="account">Must include navigation property values for this.FileDropUserPermissionGroup.FileDrop</param>
        /// <param name="canonicalPath"></param>
        /// <returns></returns>
        internal async Task<DirectoryContentModel> CreateFolderContentModelAsync(Guid fileDropId, SftpAccount account, string canonicalPath)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetService<ApplicationDbContext>();

                FileDropDirectory thisDirectory = await context.FileDropDirectory
                                                    .Include(d => d.ChildDirectories)
                                                    .Include(d => d.Files)
                                                    .Where(d => d.FileDropId == fileDropId)
                                                    .Where(d => EF.Functions.ILike(d.CanonicalFileDropPath, canonicalPath))
                                                    .SingleOrDefaultAsync();

                try
                {
                    var model = new DirectoryContentModel
                    {
                        ThisDirectory = new FileDropDirectoryModel(thisDirectory),
                        Directories = thisDirectory.ChildDirectories.Select(d => new FileDropDirectoryModel(d)).OrderBy(d => d.CanonicalPath).ToList(),
                        Files = thisDirectory.Files.Select(f => new FileDropFileModel(f)).OrderBy(f => f.FileName).ToList(),
                    };
                    model.CurrentUserPermissions.ReadAccess = account.FileDropUserPermissionGroup.ReadAccess;
                    model.CurrentUserPermissions.WriteAccess = account.FileDropUserPermissionGroup.WriteAccess;
                    model.CurrentUserPermissions.DeleteAccess = account.FileDropUserPermissionGroup.DeleteAccess;
                    return model;
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApplicationException($"Requested directory with canonical path {canonicalPath} not found in FileDrop {account.FileDropUserPermissionGroup.FileDrop.Name} (Id {account.FileDropUserPermissionGroup.FileDrop.Id})");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error building return model, parameters: canonical path {canonicalPath}, FileDrop {account.FileDropUserPermissionGroup.FileDrop.Name} (Id {account.FileDropUserPermissionGroup.FileDrop.Id})", ex);
                }
            }
        }
    }
}
