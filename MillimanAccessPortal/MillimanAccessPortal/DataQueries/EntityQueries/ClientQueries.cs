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
        private BasicClient _findClient(Guid id)
        {
            return _dbContext.Client
                .Where(c => c.Id == id)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
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
        private List<BasicClient> _selectClientWhereRole(ApplicationUser user, RoleEnum role)
        {
            return _dbContext.UserRoleInClient
                .Where(r => r.User.Id == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Select(r => r.Client)
                .OrderBy(c => c.Name)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .ToList();
        }

        /// <summary>
        /// Add stats for each client in a list
        /// </summary>
        /// <param name="clients">List of clients</param>
        /// <returns>List of clients with stats</returns>
        private List<BasicClientWithStats> _withStats(List<BasicClient> clients)
        {
            var clientsWith = new List<BasicClientWithStats> { };
            foreach (var client in clients)
            {
                var clientWith = new BasicClientWithStats
                {
                    Id = client.Id,
                    Name = client.Name,
                    Code = client.Code,
                };

                clientWith.ContentItemCount = _dbContext.RootContentItem
                    .Where(i => i.ClientId == client.Id)
                    .Count();
                clientWith.UserCount = _dbContext.UserClaims
                    .Where(m => m.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(m => m.ClaimValue == client.Id.ToString())
                    .Count();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }

        /// <summary>
        /// Add a list of eligible users to a client with stats model
        /// </summary>
        /// <param name="clients">List of clients</param>
        /// <returns>List of clients with stats and eligble users</returns>
        private List<BasicClientWithEligibleUsers> _withEligibleUsers(List<BasicClientWithStats> clients)
        {
            var clientsWith = new List<BasicClientWithEligibleUsers> { };
            foreach (var client in clients)
            {
                var clientWith = new BasicClientWithEligibleUsers
                {
                    Id = client.Id,
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
        /// Select clients for a specific user and role with stats and list of eligible users
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

            var clients = _selectClientWhereRole(user, role);
            var clientsWithStats = _withStats(clients);
            var clientsWithEligibleUsers = _withEligibleUsers(clientsWithStats);

            return clientsWithEligibleUsers;
        }

        /// <summary>
        /// Select a single client by ID with stats
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>Client with stats</returns>
        internal BasicClientWithStats SelectClientWithStats(Guid clientId)
        {
            var client = _findClient(clientId);
            var clientWithStats = _withStats(new List<BasicClient> { client })
                .SingleOrDefault();

            return clientWithStats;
        }
    }
}
