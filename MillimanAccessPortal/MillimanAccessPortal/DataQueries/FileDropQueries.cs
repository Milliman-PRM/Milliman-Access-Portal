/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An DI injectable resource containing queries directly related to FileDrop controller actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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

        public FileDropQueries(
            ApplicationDbContext dbContextArg,
            ClientQueries clientQueries,
            HierarchyQueries hierarchyQueries,
            UserQueries userQueries)
        {
            _dbContext = dbContextArg;
            _clientQueries = clientQueries;
            _hierarchyQueries = hierarchyQueries;
            _userQueries = userQueries;
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
            FileDrop fileDrop = await _dbContext.FileDrop
                                                .SingleOrDefaultAsync(fd => fd.Id == model.FileDropId);

            List<FileDropUserPermissionGroup> groupsToRemove = await _dbContext.FileDropUserPermissionGroup
                                                                               .Include(g => g.SftpAccounts)
                                                                               .Where(fd => model.RemovedPermissionGroupIds.Contains(fd.Id))
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

            // model validation
            if (fileDrop == null ||
                groupsToRemove.Any(g => g.FileDropId != model.FileDropId) ||
                groupsToRemove.Count != model.RemovedPermissionGroupIds.Count ||
                groupsToUpdate.Any(g => g.FileDropId != model.FileDropId) ||
                groupsToUpdate.Count != model.UpdatedPermissionGroups.Keys.Count ||
                sftpAccountIdsOfUsersRemovedInUpdates.Count != model.UpdatedPermissionGroups.SelectMany(u => u.Value.UsersRemoved).Count())
            {
                return null;
            }

            // Handle removed groups.  This unassigns accounts so they can be reassigned by leveraging ON DELETE SET NULL of the FK relationship
            _dbContext.FileDropUserPermissionGroup.RemoveRange(groupsToRemove);

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
                    //or removedAccount.FileDropUserPermissionGroupId = null;
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
                                                            .ToList();

                List<Guid> userIdsRequiringNewAccount = userIdList.Except(existinguserAccountsToAdd.Select(a => a.ApplicationUserId.Value)).ToList();
                List<ApplicationUser> usersRequiringNewAccount = _dbContext.ApplicationUser
                                                                           .Where(u => userIdsRequiringNewAccount.Contains(u.Id))
                                                                           .ToList();

                foreach (Guid userIdToAdd in userIdList)
                {
                    SftpAccount accountToAdd = userIdsRequiringNewAccount.Contains(userIdToAdd)
                        ? new SftpAccount(model.FileDropId)
                            {
                                ApplicationUserId = userIdToAdd,
                                IsSuspended = false,
                                UserName = usersRequiringNewAccount.Single(u => u.Id == userIdToAdd).UserName
                            }
                        : existinguserAccountsToAdd.SingleOrDefault(a => userIdsRequiringNewAccount.Contains(a.ApplicationUserId.Value));

                    updatedGroupRecord.SftpAccounts.Add(accountToAdd);
                }

                // TODO later. Handle non-user accounts added to this group.  We'll need a richer request model to support that. 
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
                    }

                    newFileDropUserPermissionGroup.SftpAccounts.Add(userSftpAccount);
                    _dbContext.FileDropUserPermissionGroup.Add(newFileDropUserPermissionGroup);
                }
            }

            _dbContext.SaveChanges();

            return GetPermissionGroupsModelForFileDrop(model.FileDropId, fileDrop.ClientId);
        }

    }
}
