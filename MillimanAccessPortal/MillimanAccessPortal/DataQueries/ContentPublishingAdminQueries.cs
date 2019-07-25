/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Queries to support ContentPublishingController actions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by content access admin actions
    /// </summary>
    public class ContentPublishingAdminQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly UserQueries _userQueries;

        public ContentPublishingAdminQueries(
            ClientQueries clientQueries,
            ContentItemQueries contentItemQueries,
            UserQueries userQueries,
            ApplicationDbContext dbContextArg
            )
        {
            _clientQueries = clientQueries;
            _contentItemQueries = contentItemQueries;
            _userQueries = userQueries;
            _dbContext = dbContextArg;
        }

        internal Dictionary<Guid, BasicClientWithCardStats> GetAuthorizedClients(ApplicationUser user, RoleEnum role)
        {
            List<Client> clients = _dbContext.UserRoleInClient
                                             .Where(urc => urc.UserId == user.Id && urc.Role.RoleEnum == role)
                                             .Select(urc => urc.Client)
                                             .ToList();
            clients = AddUniqueAncestorClientsNonInclusiveOf(clients);

            List<BasicClientWithCardStats> returnList = new List<BasicClientWithCardStats>();

            foreach (Guid authorizedClientId in clients.Select(c => c.Id))
            {
                returnList.Add(_clientQueries.SelectClientWithCardStats(authorizedClientId, role, user.Id));
            }

            return returnList.ToDictionary(c => c.Id);
        }

        internal List<Client> AddUniqueAncestorClientsNonInclusiveOf(IEnumerable<Client> children)
        {
            HashSet<Client> returnSet = children.ToHashSet(new IdPropertyComparer<Client>());

            foreach (Client client in children)
            {
                FindAncestorClients(client).ForEach(c => returnSet.Add(c));
            }
            return returnSet.ToList();
        }

        private List<Client> FindAncestorClients(Client client)
        {
            List<Client> returnObject = new List<Client>();
            if (client.ParentClientId.HasValue && client.ParentClientId.Value != default)
            {
                Client parent = _dbContext.Client.Find(client.ParentClientId);
                returnObject.Add(parent);
                returnObject.AddRange(FindAncestorClients(parent));
            }
            return returnObject;
        }
    }
}
