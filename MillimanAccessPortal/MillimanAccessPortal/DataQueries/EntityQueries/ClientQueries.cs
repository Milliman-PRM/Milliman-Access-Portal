using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries
{
    public class ClientQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

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
        private List<BasicClient> _selectClientWhereRole(ApplicationUser user, RoleEnum role)
        {
            return _dbContext.UserRoleInClient
                .Where(r => r.User.Id == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Select(r => r.Client)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .ToList();
        }
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
        private List<BasicClientWithEligibleUsers> _withEligibleUsers(
            List<BasicClientWithStats> clients)
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

        internal List<BasicClientWithEligibleUsers> SelectClientsWithEligibleUsers(
            ApplicationUser user, RoleEnum role)
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
        internal BasicClientWithStats SelectClientWithStats(Guid id)
        {
            var client = _findClient(id);
            var clientWithStats = _withStats(new List<BasicClient> { client })
                .SingleOrDefault();

            return clientWithStats;
        }
    }
}
