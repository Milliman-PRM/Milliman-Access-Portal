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
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

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

        public async Task<ClientSummaryModel> GetClientSummaryAsync(Guid clientId)
        {
            Client client = await _dbContext.Client
                                            .Include(c => c.ProfitCenter)
                                            .Where(c => c.Id == clientId)
                                            .Where(c => c.Id == clientId)
                                            .Where(c => c.Id == clientId)
                                            .SingleOrDefaultAsync();
            List<ApplicationUser> clientAdmins = await _dbContext.UserRoleInClient
                                                                 .Where(urc => urc.ClientId == clientId)
                                                                 .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                                 .Select(urc => urc.User)
                                                                 .ToListAsync();
            List<ApplicationUser> profitCenterAdmins = await _dbContext.UserRoleInProfitCenter
                                                                       .Where(urp => urp.ProfitCenterId == client.ProfitCenterId)
                                                                       .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                                                                       .Select(urp => urp.User)
                                                                       .ToListAsync();

            var returnModel = new ClientSummaryModel
            {
                ClientName = client.Name,
                ClientCode = client.ClientCode,
                AssignedProfitCenter = client.ProfitCenter.Name,
                LastReviewDate = client.LastAccessReview.LastReviewDateTimeUtc,
                LastReviewedBy = client.LastAccessReview.UserName,
                PrimaryContactName = client.ContactName,
                PrimaryContactEmail = client.ContactEmail,
                ReviewDueDate = client.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(_appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")),
            };
            clientAdmins.ForEach(ca => returnModel.ClientAdmins.Add(new ClientActorModel { UserName = ca.UserName, UserEmail = ca.Email }));
            profitCenterAdmins.ForEach(pca => returnModel.ProfitCenterAdmins.Add(new ClientActorModel { UserName = pca.UserName, UserEmail = pca.Email }));

            return returnModel;
        }
    }
}
