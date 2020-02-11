using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Provides queries related to clients.
    /// </summary>
    public class ClientQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;

        public ClientQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
        }

        #region private queries
        /// <summary>
        /// Find a client by ID
        /// </summary>
        /// <param name="id">Client ID</param>
        /// <returns>Client</returns>
        private BasicClient FindClient(Guid id)
        {
            return _dbContext.Client
                .Where(c => c.Id == id)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    ParentId = c.ParentClientId,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .SingleOrDefault();
        }

        /// <summary>
        /// Select all clients where user has a specific role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <returns>Clients where User has Role</returns>
        private List<BasicClient> SelectClientWhereRole(ApplicationUser user, RoleEnum role)
        {
            return _dbContext.UserRoleInClient
                .Where(r => r.User.Id == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Select(r => r.Client)
                .OrderBy(c => c.Name)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    ParentId = c.ParentClientId,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .ToList();
        }

        /// <summary>
        /// Select any clients that are parents to clients in the provided list.
        /// Only clients that are not in the provided list will be included.
        /// </summary>
        /// <param name="clients">List of clients for which to retrieve parents</param>
        /// <returns>List of parent clients</returns>
        private List<BasicClient> SelectParents(List<BasicClientWithEligibleUsers> clients)
        {
            var clientIds = clients.Select(c => c.Id).ToList();
            var parentIds = clients
                .Where(c => c.ParentId.HasValue)
                .Select(c => c.ParentId.Value)
                .Where(id => !clientIds.Contains(id))
                .ToList();

            return _dbContext.Client
                .Where(c => parentIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    ParentId = c.ParentClientId,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .ToList();
        }

        /// <summary>
        /// Add card stats for each client in a list
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="role"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private List<BasicClientWithCardStats> AddCardStats(List<BasicClient> clients, RoleEnum? role = null, Guid? userId = null)
        {
            var clientsWith = new List<BasicClientWithCardStats> { };
            foreach (var client in clients)
            {
                var clientWith = new BasicClientWithCardStats
                {
                    Id = client.Id,
                    ParentId = client.ParentId,
                    Name = client.Name,
                    Code = client.Code,
                };

                if (role.HasValue && userId.HasValue)
                {
                    clientWith.CanManage = _dbContext.UserRoleInClient.Any(r => r.ClientId == client.Id && r.Role.RoleEnum == role.Value && r.UserId == userId.Value);
                }
                clientWith.ContentItemCount = _dbContext.RootContentItem
                    .Count(i => i.ClientId == client.Id);
                clientWith.UserCount = _dbContext.UserRoleInClient
                    .Where(r => r.ClientId == client.Id)
                    .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                    .Count();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }

        /// <summary>
        /// Add a list of eligible users to a client with card stats model
        /// </summary>
        /// <param name="clients">List of clients</param>
        /// <returns>List of clients with card stats and eligble users</returns>
        private List<BasicClientWithEligibleUsers> WithEligibleUsers(List<BasicClientWithCardStats> clients)
        {
            var clientsWith = new List<BasicClientWithEligibleUsers> { };
            foreach (var client in clients)
            {
                var clientWith = new BasicClientWithEligibleUsers
                {
                    Id = client.Id,
                    ParentId = client.ParentId,
                    Name = client.Name,
                    Code = client.Code,
                    ContentItemCount = client.ContentItemCount,
                    UserCount = client.UserCount,
                };

                clientWith.EligibleUsers = _dbContext.UserRoleInClient
                    .Where(r => r.ClientId == client.Id)
                    .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                    .Select(r => r.UserId)
                    .ToList();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }
        #endregion

        /// <summary>
        /// Select clients for a specific user and role with card stats and list of eligible users
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <returns>List of clients</returns>
        internal List<BasicClientWithEligibleUsers> SelectClientsWithEligibleUsers(ApplicationUser user, RoleEnum role)
        {
            if (user == null)
            {
                return new List<BasicClientWithEligibleUsers> { };
            }

            var clients = SelectClientWhereRole(user, role);
            var clientsWithStats = AddCardStats(clients);
            var clientsWithEligibleUsers = WithEligibleUsers(clientsWithStats);

            return clientsWithEligibleUsers;
        }

        /// <summary>
        /// Select missing parent clients for a list of clients
        /// </summary>
        /// <param name="children">Clients whose parents to retrieve</param>
        /// <returns>List of parent clients</returns>
        internal List<BasicClientWithCardStats> SelectParentClients(List<BasicClientWithEligibleUsers> children)
        {
            if (children == null)
            {
                return new List<BasicClientWithCardStats> { };
            }

            var clients = SelectParents(children);
            var clientsWithStats = AddCardStats(clients);

            return clientsWithStats;
        }

        /// <summary>
        /// Select a single client by ID with card stats
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>Client with card stats</returns>
        internal BasicClientWithCardStats SelectClientWithCardStats(Guid clientId, RoleEnum? role = null, Guid? userId = null)
        {
            var client = FindClient(clientId);
            var clientWithStats = AddCardStats(new List<BasicClient> { client }, role, userId)
                .SingleOrDefault();

            return clientWithStats;
        }

        /// <summary>
        /// Generate a single client model with card stats for publishing view client tree
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>Client with card stats</returns>
        internal BasicClientWithCardStats SelectClientWithPublishingCardStats(Client client, RoleEnum? role = null, Guid? userId = null)
        {
            var clientWithStats = (BasicClientWithCardStats)client;

            if (role.HasValue && userId.HasValue)
            {
                clientWithStats.CanManage = _dbContext.UserRoleInClient.Any(r => r.ClientId == client.Id &&
                                                                                 r.Role.RoleEnum == role.Value &&
                                                                                 r.UserId == userId.Value);
            }

            clientWithStats.ContentItemCount = _dbContext.RootContentItem.Count(i => i.ClientId == client.Id);

            // In publishing admin view client users include everyone who is assigned to a selection group of any content in the client
            clientWithStats.UserCount = _dbContext.UserInSelectionGroup
                                                  .Where(g => g.SelectionGroup.RootContentItem.ClientId == client.Id)
                                                  .Select(r => r.UserId)
                                                  .Distinct()
                                                  .Count();

            return clientWithStats;
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
