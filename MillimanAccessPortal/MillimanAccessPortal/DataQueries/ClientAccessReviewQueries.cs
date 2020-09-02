/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An injectable service that runs database queries in support of the ClientAccessReviewController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.ClientAccessReview;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MillimanAccessPortal.DataQueries
{
    public class ClientAccessReviewQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly UserQueries _userQueries;
        private readonly IConfiguration _appConfig;

        public ClientAccessReviewQueries(
            ClientQueries clientQueriesArg,
            ContentItemQueries contentItemQueriesArg,
            UserQueries userQueriesArg,
            ApplicationDbContext dbContextArg,
            IConfiguration appConfigArg)
        {
            _clientQueries = clientQueriesArg;
            _contentItemQueries = contentItemQueriesArg;
            _userQueries = userQueriesArg;
            _dbContext = dbContextArg;
            _appConfig = appConfigArg;
        }

        public async Task<ClientReviewClientsModel> GetClientModelAsync(ApplicationUser user)
        {
            var clients = (await _dbContext.UserRoleInClient
                                           .Where(r => r.User.Id == user.Id)
                                           .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                           .OrderBy(r => r.Client.Name)
                                           .Select(c => c.Client)
                                           .ToListAsync())
                                           .ConvertAll(c => new ClientReviewModel(c, _appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")));
            var clientIds = clients.Select(c => c.Id).ToList();
            var parentIds = clients.Where(c => c.ParentId.HasValue)
                                   .Select(c => c.ParentId.Value)
                                   .Where(id => !clientIds.Contains(id))
                                   .ToList();
            var parents = await _dbContext.Client
                                          .Where(c => parentIds.Contains(c.Id))
                                          .OrderBy(c => c.Name)
                                          .Select(c => new ClientReviewModel(c, _appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")))
                                          .ToListAsync();

            return new ClientReviewClientsModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                ParentClients = parents.ToDictionary(p => p.Id),
            };
        }
    }
}
