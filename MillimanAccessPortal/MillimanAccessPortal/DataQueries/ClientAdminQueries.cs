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
using MillimanAccessPortal.Models.ClientAdminViewModels;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.DataQueries
{
    /// <summary>
    /// Queries used by client admin actions
    /// </summary>
    public class ClientAdminQueries
    {
    private readonly ApplicationDbContext _dbContext;
    private readonly ClientQueries _clientQueries;
        private readonly UserQueries _userQueries;

        public ClientAdminQueries(
            ApplicationDbContext dbContext,
            ClientQueries clientQueries,
            UserQueries userQueries)
        {
            _dbContext = dbContext;
            _clientQueries = clientQueries;
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

        public async Task<List<AuthorizedProfitCenterModel>> GetAuthorizedProfitCentersListAsync(ApplicationUser user)
        {
            List<AuthorizedProfitCenterModel> AuthorizedProfitCenterList = new List<AuthorizedProfitCenterModel>();
            foreach (var AuthorizedProfitCenter in (await _dbContext.UserRoleInProfitCenter
                                                       .Include(urpc => urpc.Role)
                                                       .Include(urpc => urpc.ProfitCenter)
                                                       .Where(urpc => urpc.Role.RoleEnum == RoleEnum.Admin
                                                                   && urpc.UserId == user.Id)
                                                       .Select(urpc => urpc.ProfitCenter)
                                                       .ToListAsync())
                                               .Distinct(new IdPropertyComparer<ProfitCenter>()))
            {
              AuthorizedProfitCenterList.Add(new AuthorizedProfitCenterModel(AuthorizedProfitCenter));
            }

            return AuthorizedProfitCenterList;
        }

        public async Task<SaveNewClientResponseModel> GetNewClientResponseModelAsync(ApplicationUser user, Guid clientId)
        {
            var clientResponseModel = await this.GetAuthorizedClientsModelAsync(user);
            Client newClient = await _dbContext.Client
                                   .Include(c => c.ProfitCenter)
                                   .FirstOrDefaultAsync(c => c.Id == clientId);

            SaveNewClientResponseModel ReturnModel = new SaveNewClientResponseModel
            {
              NewClient = (ClientDetail) newClient,
              Clients = clientResponseModel.Clients,
            };

            return ReturnModel;
        }
    }
}