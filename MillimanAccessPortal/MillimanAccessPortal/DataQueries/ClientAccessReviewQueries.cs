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
using Microsoft.AspNetCore.Identity;

namespace MillimanAccessPortal.DataQueries
{
    public class ClientAccessReviewQueries
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ClientQueries _clientQueries;
        private readonly ContentItemQueries _contentItemQueries;
        private readonly UserQueries _userQueries;
        private readonly IConfiguration _appConfig;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientAccessReviewQueries(
            ClientQueries clientQueriesArg,
            ContentItemQueries contentItemQueriesArg,
            UserQueries userQueriesArg,
            ApplicationDbContext dbContextArg,
            IConfiguration appConfigArg,
            UserManager<ApplicationUser> userManagerArg)
        {
            _clientQueries = clientQueriesArg;
            _contentItemQueries = contentItemQueriesArg;
            _userQueries = userQueriesArg;
            _dbContext = dbContextArg;
            _appConfig = appConfigArg;
            _userManager = userManagerArg;
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
            clientAdmins.ForEach(ca => returnModel.ClientAdmins.Add(new ClientActorModel(ca)));
            profitCenterAdmins.ForEach(pca => returnModel.ProfitCenterAdmins.Add(new ClientActorModel(pca)));

            return returnModel;
        }

        public async Task<ClientAccessReviewModel> GetClientAccessReviewModel(Guid clientId)
        {
            Client client = await _dbContext.Client
                                            .Include(c => c.ProfitCenter)
                                            .SingleOrDefaultAsync(c => c.Id == clientId);
            IEnumerable<ClientActorReviewModel> memberUsers = (await _userManager.GetUsersForClaimAsync(new System.Security.Claims.Claim("ClientMembership", client.Id.ToString())))
                .Select(u => 
                {
                    Task<DateTime?> LastLoginTask = AuditLogLib.AuditLogger.GetUserLastLogin(u.UserName);
                    Task<List<RoleEnum>> authorizedRolesTask = _dbContext.UserRoleInClient
                                                                         .Where(urc => urc.UserId == u.Id)
                                                                         .Where(urc => urc.ClientId == client.Id)
                                                                         .Select(urc => urc.Role.RoleEnum)
                                                                         .ToListAsync();
                    Task.WaitAll(LastLoginTask, authorizedRolesTask);
                    return new ClientActorReviewModel(u)
                    {
                        LastLoginDate = LastLoginTask.Result,
                        ClientUserRoles = Enum.GetValues(typeof(RoleEnum))
                                              .OfType<RoleEnum>()
                                              .Select(r => new KeyValuePair<RoleEnum, bool>(r, authorizedRolesTask.Result.Contains(r)))
                                              .ToDictionary(p => p.Key, p => p.Value),
                    };
                });

            List<RootContentItem> contentItems = await _dbContext.RootContentItem
                                                                 .Include(c => c.ContentType)
                                                                 .Where(c => c.ClientId == client.Id)
                                                                 .ToListAsync();

            List<ApplicationUser> profitCenterAdmins = await _dbContext.UserRoleInProfitCenter
                                                                       .Where(urp => urp.ProfitCenterId == client.ProfitCenterId)
                                                                       .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                                                                       .Select(urp => urp.User)
                                                                       .ToListAsync();

            List<FileDrop> fileDrops = await _dbContext.FileDrop
                                                       .Include(d => d.PermissionGroups)
                                                           .ThenInclude(g => g.SftpAccounts)
                                                               .ThenInclude(a => a.ApplicationUser)
                                                       .Where(d => d.ClientId == client.Id)
                                                       .ToListAsync();

            var returnModel = new ClientAccessReviewModel
            {
                Id = client.Id,
                ClientName = client.Name,
                ClientCode = client.ClientCode,
                ClientAdmins = memberUsers.Where(m => m.ClientUserRoles.ContainsKey(RoleEnum.Admin))
                                          .Where(m => m.ClientUserRoles[RoleEnum.Admin])
                                          .Select(m => new ClientActorModel(m))
                                          .ToList(),
                AssignedProfitCenterName = client.ProfitCenter.Name,
                AttestationLanguage = _appConfig.GetValue<string>("ClientReviewAttestationLanguage"),
                ClientAccessReviewId = Guid.NewGuid(),
            };
            returnModel.ApprovedEmailDomainList.AddRange(client.AcceptedEmailDomainList);
            returnModel.ApprovedEmailExceptionList.AddRange(client.AcceptedEmailAddressExceptionList);
            profitCenterAdmins.ForEach(pca => returnModel.ProfitCenterAdmins.Add(new ClientActorModel(pca)));
            returnModel.MemberUsers.AddRange(memberUsers);
            contentItems.ForEach(c =>
            {
                var relatedGroups = _dbContext.SelectionGroup
                                              .Where(g => g.RootContentItemId == c.Id)
                                              .ToList();
                var contentModel = new ClientContentItemModel
                {
                    ContentItemName = c.ContentName,
                    ContentType = c.ContentType.TypeEnum.GetDisplayNameString(),
                    IsSuspended = c.IsSuspended,
                    LastPublishedDate = _dbContext.ContentPublicationRequest
                                                  .Where(r => r.RootContentItemId == c.Id)
                                                  .Where(r => r.RequestStatus == PublicationStatus.Confirmed)
                                                  .OrderByDescending(r => r.CreateDateTimeUtc)
                                                  .FirstOrDefault()
                                                  ?.CreateDateTimeUtc,
                };
                relatedGroups.ForEach(g =>
                {
                    var groupModel = new ClientContentItemSelectionGroupModel 
                    { 
                        SelectionGroupName = g.GroupName, 
                        IsSuspended = g.IsSuspended 
                    };
                    groupModel.AuthorizedUsers.AddRange(_dbContext.UserInSelectionGroup
                                                                  .Where(usg => usg.SelectionGroupId == g.Id)
                                                                  .Select(usg => new ClientActorModel(usg.User)));
                    contentModel.SelectionGroups.Add(groupModel);
                });

                returnModel.ContentItems.Add(contentModel);
            });
            fileDrops.ForEach(d =>
            {
                ClientFileDropModel fileDropModel = new ClientFileDropModel { FileDropName = d.Name };
                foreach (FileDropUserPermissionGroup group in d.PermissionGroups)
                {
                    fileDropModel.PermissionGroups.Add(new ClientFileDropPermissionGroupModel 
                    {
                        PermissionGroupName = group.Name,
                        Permissions = new Dictionary<string, bool> { { "Read", group.ReadAccess }, { "Write", group.WriteAccess }, { "Delete", group.DeleteAccess } },
                        AuthorizedMapUsers = group.SftpAccounts.Where(a => a.ApplicationUserId.HasValue).Select(a => new ClientActorModel(a.ApplicationUser)).ToList(),
                        AuthorizedServiceAccounts = group.SftpAccounts.Where(a => !a.ApplicationUserId.HasValue).Select(a => new ClientActorModel(a)).ToList(),
                    });
                }
                returnModel.FileDrops.Add(fileDropModel);
            });

            return returnModel;
        }
    }
}
