using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.ClientModels;
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
        private readonly ClientQueries _clientQueries;
        private readonly HierarchyQueries _hierarchyQueries;
        private readonly UserQueries _userQueries;

        public FileDropQueries(
            ClientQueries clientQueries,
            HierarchyQueries hierarchyQueries,
            UserQueries userQueries)
        {
            _clientQueries = clientQueries;
            _hierarchyQueries = hierarchyQueries;
            _userQueries = userQueries;
        }

        /// <summary>
        /// Select all clients for which the current user can administer access.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        public ClientsResponseModel GetAuthorizedClientsModel(ApplicationUser user)
        {
            // TODO: Flesh this out since this will necessarily need to be more 
            //    complicated (i.e. all FileDropAdmins, and clients where a FileDropUser has access to a FileDrop)
            var clients = _clientQueries.SelectClientsWithEligibleUsers(user, RoleEnum.FileDropAdmin);
            var parentClients = _clientQueries.SelectParentClients(clients);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = _userQueries.SelectUsersWhereEligibleClientIn(clientIds);

            return new ClientsResponseModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                ParentClients = parentClients.ToDictionary(c => c.Id),
                Users = users.ToDictionary(u => u.Id),
            };
        }
    }
}
