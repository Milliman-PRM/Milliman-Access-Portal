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
                                                     AssignedSftpAccountIds = accounts.Select(a => a.Id).ToList(),
                                                     AssignedMapUserIds = accounts.Where(a => a.ApplicationUserId.HasValue).Select(a => a.ApplicationUserId.Value).ToList(),
                                                 };
                                             })
                                             .ToDictionary(m => m.Id),

            };

            return returnModel;
        }

        internal async Task<PermissionGroupsModel> UpdatePermissionGroups(UpdatePermissionGroupsModel model)
        {
            var fileDrop = await _dbContext.FileDrop.SingleOrDefaultAsync(fd => fd.Id == model.FileDropId);

            // First need to free up all users who are in deleted 
            foreach (var removedPermissionGroup in _dbContext.FileDropUserPermissionGroup.Where(fd => model.RemovedPermissionGroupIds.Contains(fd.Id)).ToList())
            {
                _dbContext.FileDropUserPermissionGroup.Remove(removedPermissionGroup);
            }



            return GetPermissionGroupsModelForFileDrop(model.FileDropId, fileDrop.ClientId);
        }

    }
}
