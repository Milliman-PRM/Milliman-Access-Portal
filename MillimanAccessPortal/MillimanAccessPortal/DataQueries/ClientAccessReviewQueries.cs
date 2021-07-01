/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An injectable service that runs database queries in support of the ClientAccessReviewController
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
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
            List<ClientReviewModel> clients = (await _dbContext.UserRoleInClient
                                                               .Where(r => r.User.Id == user.Id)
                                                               .Where(r => r.Role.RoleEnum == RoleEnum.Admin)
                                                               .OrderBy(r => r.Client.Name)
                                                               .Select(c => c.Client)
                                                               .ToListAsync())
                                                               .ConvertAll(c => new ClientReviewModel(c, 
                                                                                                      _appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"), 
                                                                                                      _appConfig.GetValue<int>("ClientReviewEarlyWarningDays"), 
                                                                                                      _appConfig.GetValue<int>("ClientReviewNotificationTimeOfDayHourUtc"), 
                                                                                                      user.TimeZoneId));
            List<Guid> clientIds = clients.Select(c => c.Id).ToList();
            List<Guid> parentIds = clients.Where(c => c.ParentId.HasValue)
                                          .Select(c => c.ParentId.Value)
                                          .Where(id => !clientIds.Contains(id))
                                          .ToList();
            List<ClientReviewModel> parents = _dbContext.Client
                                                        .Where(c => parentIds.Contains(c.Id))
                                                        .OrderBy(c => c.Name)
                                                        .AsEnumerable()
                                                        .Select(c => new ClientReviewModel(c, 
                                                                                           _appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"), 
                                                                                           _appConfig.GetValue<int>("ClientReviewEarlyWarningDays"), 
                                                                                           _appConfig.GetValue<int>("ClientReviewNotificationTimeOfDayHourUtc"), 
                                                                                           user.TimeZoneId))
                                                        .ToList();

            return new ClientReviewClientsModel
            {
                Clients = clients.ToDictionary(c => c.Id),
                ParentClients = parents.ToDictionary(p => p.Id),
            };
        }

        public async Task<ClientSummaryModel> GetClientSummaryAsync(Guid clientId, string userTimeZone)
        {
            Client client = await _dbContext.Client
                                            .Include(c => c.ProfitCenter)
                                            .Where(c => c.Id == clientId)
                                            .SingleOrDefaultAsync();
            List<ApplicationUser> clientAdmins = await _dbContext.UserRoleInClient
                                                                 .Where(urc => urc.ClientId == clientId)
                                                                 .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                                 .Select(urc => urc.User)
                                                                 .OrderBy(urc => urc.LastName)
                                                                 .ThenBy(urc => urc.FirstName)
                                                                 .ToListAsync();
            List<ApplicationUser> profitCenterAdmins = await _dbContext.UserRoleInProfitCenter
                                                                       .Where(urp => urp.ProfitCenterId == client.ProfitCenterId)
                                                                       .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                                                                       .Select(urp => urp.User)
                                                                       .OrderBy(urp => urp.LastName)
                                                                       .ThenBy(urp => urp.FirstName)
                                                                       .ToListAsync();

            ApplicationUser lastApprover = await _userManager.FindByNameAsync(client.LastAccessReview.UserName);
            var returnModel = new ClientSummaryModel
            {
                ClientName = client.Name,
                ClientCode = client.ClientCode,
                AssignedProfitCenter = client.ProfitCenter.Name,
                LastReviewDate = GlobalFunctions.UtcToLocalString(client.LastAccessReview.LastReviewDateTimeUtc, userTimeZone),
                LastReviewedBy = lastApprover == null  // Usually null indicates the username is the default "N/A", indicating no previous review
                    ? new ClientActorModel { Name = client.LastAccessReview.UserName }  // This avoids potential null reference exception in the ClientActorModel cast operator
                    : (ClientActorModel)lastApprover,
                PrimaryContactName = client.ContactName,
                PrimaryContactEmail = client.ContactEmail,
                ReviewDueDate = GlobalFunctions.UtcToLocalString(client.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(_appConfig.GetValue<int>("ClientReviewRenewalPeriodDays")), userTimeZone),
            };
            clientAdmins.ForEach(ca => returnModel.ClientAdmins.Add((ClientActorModel)ca));
            profitCenterAdmins.ForEach(pca => returnModel.ProfitCenterAdmins.Add((ClientActorModel)pca));

            return returnModel;
        }

        public async Task<ClientAccessReviewModel> GetClientAccessReviewModel(Guid clientId)
        {
            Client client = await _dbContext.Client
                                            .Include(c => c.ProfitCenter)
                                            .SingleOrDefaultAsync(c => c.Id == clientId);
            int disableInactiveUserMonths = _appConfig.GetValue<int>("DisableInactiveUserMonths");
            int disableInactiveUserWarningDays = _appConfig.GetValue<int>("DisableInactiveUserWarningDays");

            IEnumerable<ClientActorReviewModel> memberUsers = (await _userManager.GetUsersForClaimAsync(new System.Security.Claims.Claim("ClientMembership", client.Id.ToString())))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => 
                {
                    List<RoleEnum> authorizedRoles = _dbContext.UserRoleInClient
                                                               .Where(urc => urc.UserId == u.Id)
                                                               .Where(urc => urc.ClientId == client.Id)
                                                               .Select(urc => urc.Role.RoleEnum)
                                                               .ToList();
                    var memberModel = (ClientActorReviewModel)u;
                    memberModel.ClientUserRoles = Enum.GetValues(typeof(RoleEnum))
                                                      .OfType<RoleEnum>()
                                                      .Select(r => new KeyValuePair<RoleEnum, bool>(r, authorizedRoles.Contains(r)))
                                                      .ToDictionary(p => p.Key, p => p.Value);                    
                    memberModel.DisableAccountDate = memberModel.LastLoginDate?.AddMonths(disableInactiveUserMonths);
                    memberModel.IsAccountDisabled = memberModel.LastLoginDate < DateTime.UtcNow.Date.AddMonths(-disableInactiveUserMonths);
                    memberModel.IsAccountNearDisabled = !memberModel.IsAccountDisabled && memberModel.LastLoginDate < DateTime.UtcNow.Date.AddMonths(-disableInactiveUserMonths).AddDays(disableInactiveUserWarningDays);
                    return memberModel;
                });

            List<RootContentItem> contentItems = await _dbContext.RootContentItem
                                                                 .Include(c => c.ContentType)
                                                                 .Where(c => c.ClientId == client.Id)
                                                                 .OrderBy(c => c.ContentName)
                                                                 .ToListAsync();

            List<ApplicationUser> profitCenterAdmins = await _dbContext.UserRoleInProfitCenter
                                                                       .Where(urp => urp.ProfitCenterId == client.ProfitCenterId)
                                                                       .Where(urp => urp.Role.RoleEnum == RoleEnum.Admin)
                                                                       .Select(urp => urp.User)
                                                                       .OrderBy(u => u.LastName)
                                                                       .ThenBy(u => u.FirstName)
                                                                       .ToListAsync();

            List<FileDrop> fileDrops = await _dbContext.FileDrop
                                                       .Include(d => d.PermissionGroups)
                                                           .ThenInclude(g => g.SftpAccounts)
                                                               .ThenInclude(a => a.ApplicationUser)
                                                       .Where(d => d.ClientId == client.Id)
                                                       .OrderBy(d => d.Name)
                                                       .ToListAsync();

            var returnModel = new ClientAccessReviewModel
            {
                Id = client.Id,
                ClientName = client.Name,
                ClientCode = client.ClientCode,
                ClientAdmins = memberUsers.Where(m => m.ClientUserRoles.ContainsKey(RoleEnum.Admin))
                                          .Where(m => m.ClientUserRoles[RoleEnum.Admin])
                                          .Select(m => (ClientActorModel)m)
                                          .ToList(),
                AssignedProfitCenterName = client.ProfitCenter.Name,
                AttestationLanguage = _appConfig.GetValue<string>("ClientReviewAttestationLanguage"),
                ClientAccessReviewId = Guid.NewGuid(),
            };
            returnModel.ApprovedEmailDomainList.AddRange(client.AcceptedEmailDomainList.OrderBy(d => d));
            returnModel.ApprovedEmailExceptionList.AddRange(client.AcceptedEmailAddressExceptionList.OrderBy(e => e));
            profitCenterAdmins.ForEach(pca => returnModel.ProfitCenterAdmins.Add((ClientActorModel)pca));
            returnModel.MemberUsers.AddRange(memberUsers);
            contentItems.ForEach(c =>
            {
                var relatedGroups = _dbContext.SelectionGroup
                                              .Where(g => g.RootContentItemId == c.Id)
                                              .OrderBy(g => g.GroupName)
                                              .ToList();
                var contentModel = new ClientContentItemModel
                {
                    Id = c.Id,
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
                                                                  .OrderBy(usg => usg.User.LastName)
                                                                  .ThenBy(usg => usg.User.FirstName)
                                                                  .Select(usg => (ClientActorModel)usg.User));
                    contentModel.SelectionGroups.Add(groupModel);
                });

                returnModel.ContentItems.Add(contentModel);
            });
            fileDrops.ForEach(d =>
            {
                ClientFileDropModel fileDropModel = new ClientFileDropModel { Id = d.Id, FileDropName = d.Name };
                foreach (FileDropUserPermissionGroup group in d.PermissionGroups.OrderByDescending(pg => pg.IsPersonalGroup).ThenBy(pg => pg.Name))
                {
                    var fdUsers = group.SftpAccounts
                        .Where(a => a.ApplicationUserId.HasValue)
                        .OrderBy(a => a.ApplicationUser.LastName)
                        .ThenBy(a => a.ApplicationUser.FirstName)
                        .Select(a => (ClientActorModel)a.ApplicationUser).ToList();
                    fdUsers.ForEach(u =>
                    {
                        u.DisableAccountDate = u.LastLoginDate?.AddMonths(disableInactiveUserMonths);
                        u.IsAccountDisabled = u.LastLoginDate < DateTime.UtcNow.Date.AddMonths(-disableInactiveUserMonths);
                        u.IsAccountNearDisabled = !u.IsAccountDisabled && u.LastLoginDate < DateTime.UtcNow.Date.AddMonths(-disableInactiveUserMonths).AddDays(disableInactiveUserWarningDays);
                    });
                    fileDropModel.PermissionGroups.Add(new ClientFileDropPermissionGroupModel
                    {
                        PermissionGroupName = group.Name,
                        IsPersonalGroup = group.IsPersonalGroup,
                        Permissions = new Dictionary<string, bool> { { "Read", group.ReadAccess }, { "Write", group.WriteAccess }, { "Delete", group.DeleteAccess } },
                        AuthorizedMapUsers = fdUsers,
                        AuthorizedServiceAccounts = group.SftpAccounts
                            .Where(a => !a.ApplicationUserId.HasValue)
                            .Select(a => (ClientActorModel)a)
                            .OrderBy(sa => sa.UserEmail).ToList(),
                    });
                }
                returnModel.FileDrops.Add(fileDropModel);
            });

            return returnModel;
        }

        public async Task<ClientReviewClientsModel> ApproveClientAccessReviewAsync(ApplicationUser currentUser, Guid clientId)
        {
            Client client = await _dbContext.Client.FindAsync(clientId);
            if (client == null)
            {
                throw new ApplicationException("Requested client not found");
            }

            client.LastAccessReview = new ClientAccessReview { LastReviewDateTimeUtc = DateTime.UtcNow, UserName = currentUser.UserName };
            await _dbContext.SaveChangesAsync();

            return await GetClientModelAsync(currentUser);
        }
    }
}
