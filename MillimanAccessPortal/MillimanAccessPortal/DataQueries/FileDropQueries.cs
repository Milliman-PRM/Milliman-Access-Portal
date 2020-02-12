/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An DI injectable resource containing queries directly related to FileDrop controller actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.FileDrop;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Select all clients for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        internal Dictionary<Guid, ClientCardModel> GetAuthorizedClientsModel(ApplicationUser user)
        {
            List<Client> clientList = _dbContext.UserRoleInClient
                                                .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == RoleEnum.FileDropAdmin)
                                                .Select(urc => urc.Client)
                                                .ToList()  // force the first query to execute
                                                .Union(
                                                    _dbContext.ApplicationUser
                                                              .Where(u => u.UserName.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase))
                                                              .SelectMany(u => u.SftpAccounts.Select(a => a.FileDropUserPermissionGroup.FileDrop.Client)),
                                                    new IdPropertyComparer<Client>()
                                                )
                                                .ToList();
            clientList = _clientQueries.AddUniqueAncestorClientsNonInclusiveOf(clientList);

            List<ClientCardModel> returnList = new List<ClientCardModel>();
            foreach (Client oneClient in clientList)
            {
                returnList.Add(GetClientCardModelAsync(oneClient, user));
            }

            return returnList.ToDictionary(c => c.Id);
        }

        private ClientCardModel GetClientCardModelAsync(Client client, ApplicationUser user)
        {
            return new ClientCardModel(client)
            {
                UserCount = _dbContext.ApplicationUser
                                      .Where(u => u.SftpAccounts
                                                   .Any(a => a.FileDropUserPermissionGroup.FileDrop.ClientId == client.Id))
                                      .ToList()
                                      .Distinct(new IdPropertyComparer<ApplicationUser>())
                                      .Count(),

                FileDropCount = _dbContext.FileDrop
                                          .Count(d => d.ClientId == client.Id),

                CanManage = _dbContext.UserRoleInClient
                                      .Any(ur => ur.ClientId == client.Id &&
                                                 ur.UserId == user.Id &&
                                                 ur.Role.RoleEnum == RoleEnum.FileDropAdmin),
            };
        }
    }
}
