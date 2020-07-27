using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries.EntityQueries;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by client admin actions
    /// </summary>
    public class ClientAdminQueries
    {
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly HierarchyQueries _hierarchyQueries;
        private readonly SelectionGroupQueries _selectionGroupQueries;
        private readonly PublicationQueries _publicationQueries;
        private readonly UserQueries _userQueries;

        public ClientAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            HierarchyQueries hierarchyQueries,
            SelectionGroupQueries selectionGroupQueries,
            PublicationQueries publicationQueries,
            UserQueries userQueries)
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
            _hierarchyQueries = hierarchyQueries;
            _selectionGroupQueries = selectionGroupQueries;
            _publicationQueries = publicationQueries;
            _userQueries = userQueries;
        }

        /// <summary>
        /// Select all clients for which the current user can administer.
        /// </summary>
        /// <param name="user">Current user</param>
        /// <returns>Response model</returns>
        public async Task<ClientsResponseModel> GetAuthorizedClientsModelAsync(ApplicationUser user)
        {
            var clients = await _clientQueries.SelectClientsWithEligibleUsersAsync(user, RoleEnum.Admin);
            var parentClients = await _clientQueries.SelectParentClientsAsync(clients);
            var clientIds = clients.ConvertAll(c => c.Id);

            var users = await _userQueries.SelectUsersWhereEligibleClientInAsync(clientIds);

            return new ClientsResponseModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                ParentClients = parentClients.ToDictionary(c => c.Id),
                Users = users.ToDictionary(u => u.Id),
            };
        }

    }
}