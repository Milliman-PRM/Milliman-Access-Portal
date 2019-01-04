using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private async Task<BasicClient> _findClient(Guid id)
        {
            return await _dbContext.Client
                .Where(c => c.Id == id)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .SingleOrDefaultAsync();
        }
        private async Task<List<BasicClient>> _selectClientWhereRole(ApplicationUser user, RoleEnum role)
        {
            return await _dbContext.UserRoleInClient
                .Where(r => r.User.Id == user.Id)
                .Where(r => r.Role.RoleEnum == role)
                .Select(r => r.Client)
                .Select(c => new BasicClient
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.ClientCode,
                })
                .ToListAsync();
        }
        private async Task<List<BasicClientWithStats>> _withStats(List<BasicClient> clients)
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

                clientWith.ContentItemCount = await _dbContext.RootContentItem
                    .Where(i => i.ClientId == client.Id)
                    .CountAsync();
                clientWith.UserCount = await _dbContext.UserClaims
                    .Where(m => m.ClaimType == ClaimNames.ClientMembership.ToString())
                    .Where(m => m.ClaimValue == client.Id.ToString())
                    .CountAsync();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }
        private async Task<List<BasicClientWithEligibleUsers>> _withEligibleUsers(
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

                clientWith.EligibleUsers = await _dbContext.UserRoleInClient
                    .Where(r => r.ClientId == client.Id)
                    .Where(r => r.Role.RoleEnum == RoleEnum.ContentUser)
                    .Select(r => r.UserId)
                    .ToListAsync();

                clientsWith.Add(clientWith);
            }
            return clientsWith;
        }

        internal async Task<List<BasicClientWithEligibleUsers>> SelectClientsWithEligibleUsers(
            ApplicationUser user, RoleEnum role)
        {
            if (user == null)
            {
                return new List<BasicClientWithEligibleUsers> { };
            }

            var clients = await _selectClientWhereRole(user, role);
            var clientsWithStats = await _withStats(clients);
            var clientsWithEligibleUsers = await _withEligibleUsers(clientsWithStats);

            return clientsWithEligibleUsers;
        }
        internal async Task<BasicClientWithStats> SelectClientWithStats(Guid id)
        {
            var client = await _findClient(id);
            var clientWithStats = (await _withStats(new List<BasicClient> { client }))
                .SingleOrDefault();

            return clientWithStats;
        }
    }
}
