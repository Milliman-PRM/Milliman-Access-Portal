using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private async Task<BasicClient> FindClientAsync(Guid id)
        {
            return await _dbContext.Client
                                   .Where(c => c.Id == id)
                                   .Select(c => new BasicClient
                                   {
                                       Id = c.Id,
                                       ParentId = c.ParentClientId,
                                       Name = c.Name,
                                       Code = c.ClientCode,
                                   })
                                   .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Select all clients where user has a specific role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        /// <returns>Clients where User has Role</returns>
        private async Task<List<BasicClient>> SelectClientWhereRoleAsync(ApplicationUser user, RoleEnum role)
        {
            return await _dbContext.UserRoleInClient
                .Where(r => r.User.Id == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .OrderBy(r => r.Client.Name)
                .Select(r => new BasicClient
                {
                    Id = r.Client.Id,
                    ParentId = r.Client.ParentClientId,
                    Name = r.Client.Name,
                    Code = r.Client.ClientCode,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Select any clients that are parents to clients in the provided list.
        /// Only clients that are not in the provided list will be included.
        /// </summary>
        /// <param name="clients">List of clients for which to retrieve parents</param>
        /// <returns>List of parent clients</returns>
        private async Task<List<BasicClient>> SelectParentsAsync(List<BasicClientWithEligibleUsers> clients)
        {
            var clientIds = clients.Select(c => c.Id).ToList();
            var parentIds = clients
                .Where(c => c.ParentId.HasValue)
                .Select(c => c.ParentId.Value)
                .Where(id => !clientIds.Contains(id))
                .ToList();

            return await _dbContext.Client
                                   .Where(c => parentIds.Contains(c.Id))
                                   .OrderBy(c => c.Name)
                                   .Select(c => new BasicClient
                                   {
                                       Id = c.Id,
                                       ParentId = c.ParentClientId,
                                       Name = c.Name,
                                       Code = c.ClientCode,
                                   })
                                   .ToListAsync();
        }

        /// <summary>
        /// Add card stats for each client in a list
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="role"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<List<BasicClientWithCardStats>> AddCardStatsAsync(List<BasicClient> clients, RoleEnum? role = null, Guid? userId = null)
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
                    clientWith.CanManage = await _dbContext.UserRoleInClient.AnyAsync(r => r.ClientId == client.Id 
                                                                                        && r.Role.RoleEnum == role.Value 
                                                                                        && r.UserId == userId.Value);
                }
                clientWith.ContentItemCount = await _dbContext.RootContentItem.CountAsync(i => i.ClientId == client.Id);
                clientWith.UserCount = await _dbContext.UserRoleInClient
                    .Where(r => r.ClientId == client.Id)
                    .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                    .CountAsync();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }

        /// <summary>
        /// Add a list of eligible users to a client with card stats model
        /// </summary>
        /// <param name="clients">List of clients</param>
        /// <returns>List of clients with card stats and eligble users</returns>
        private async Task<List<BasicClientWithEligibleUsers>> WithEligibleUsersAsync(List<BasicClientWithCardStats> clients)
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

                clientWith.EligibleUsers = await _dbContext.UserRoleInClient
                    .Where(r => r.ClientId == client.Id)
                    .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                    .Select(r => r.UserId)
                    .ToListAsync();

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
        internal async Task<List<BasicClientWithEligibleUsers>> SelectClientsWithEligibleUsersAsync(ApplicationUser user, RoleEnum role)
        {
            if (user == null)
            {
                return new List<BasicClientWithEligibleUsers> { };
            }

            var clients = await SelectClientWhereRoleAsync(user, role);
            var clientsWithStats = await AddCardStatsAsync(clients);
            var clientsWithEligibleUsers = await WithEligibleUsersAsync(clientsWithStats);

            return clientsWithEligibleUsers;
        }

        /// <summary>
        /// Select missing parent clients for a list of clients
        /// </summary>
        /// <param name="children">Clients whose parents to retrieve</param>
        /// <returns>List of parent clients</returns>
        internal async Task<List<BasicClientWithCardStats>> SelectParentClientsAsync(List<BasicClientWithEligibleUsers> children)
        {
            if (children == null)
            {
                return new List<BasicClientWithCardStats> { };
            }

            var clients = await SelectParentsAsync(children);
            var clientsWithStats = await AddCardStatsAsync(clients);

            return clientsWithStats;
        }

        /// <summary>
        /// Select a single client by ID with card stats
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>Client with card stats</returns>
        internal async Task<BasicClientWithCardStats> SelectClientWithCardStatsAsync(Guid clientId, RoleEnum? role = null, Guid? userId = null)
        {
            var client = await FindClientAsync(clientId);
            var clientWithStats = (await AddCardStatsAsync(new List<BasicClient> { client }, role, userId))
                                    .SingleOrDefault();

            return clientWithStats;
        }

        /// <summary>
        /// Generate a single client model with card stats for publishing view client tree
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>Client with card stats</returns>
        internal async Task<BasicClientWithCardStats> SelectClientWithPublishingCardStatsAsync(Client client, RoleEnum? role = null, Guid? userId = null)
        {
            var clientWithStats = (BasicClientWithCardStats)client;

            if (role.HasValue && userId.HasValue)
            {
                clientWithStats.CanManage = await _dbContext.UserRoleInClient.AnyAsync(r => r.ClientId == client.Id
                                                                                         && r.Role.RoleEnum == role.Value
                                                                                         && r.UserId == userId.Value);
            }

            clientWithStats.ContentItemCount = await _dbContext.RootContentItem.CountAsync(i => i.ClientId == client.Id);

            // In publishing admin view client users include everyone who is assigned to a selection group of any content in the client
            clientWithStats.UserCount = await _dbContext.UserInSelectionGroup
                                                        .Where(g => g.SelectionGroup.RootContentItem.ClientId == client.Id)
                                                        .Select(r => r.UserId)
                                                        .Distinct()
                                                        .CountAsync();

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
