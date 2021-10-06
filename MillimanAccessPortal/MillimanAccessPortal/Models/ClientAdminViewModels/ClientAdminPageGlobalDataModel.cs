/*
1 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing Clients and authorizations associated with actions that the current user is authorized to
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientAdminPageGlobalDataModel
    {
        public List<AuthorizedProfitCenterModel> AuthorizedProfitCenterList { get; set; } = new List<AuthorizedProfitCenterModel>();
        public string SystemDefaultWelcomeEmailText { get; set; }
        public List<string> NonLimitedDomains { get; protected set; } = GlobalFunctions.NonLimitedDomains;
        public List<string> ProhibitedDomains { get; protected set; } = GlobalFunctions.ProhibitedDomains;
        public int DefaultDomainLimit { get; set; } = GlobalFunctions.DefaultClientDomainListCountLimit;

        public static async Task<ClientAdminPageGlobalDataModel> GetClientAdminPageGlobalDataForUser(ApplicationUser CurrentUser, UserManager<ApplicationUser> UserManager, ApplicationDbContext DbContext, string SystemDefaultWelcomeEmailTextArg = null)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion

            // Instantiate working variables
            ClientAdminPageGlobalDataModel ModelToReturn = new ClientAdminPageGlobalDataModel();

            // Add all ProfitCenterManager authorizations for the current user
            foreach (var AuthorizedProfitCenter in (await DbContext.UserRoleInProfitCenter
                                                                   .Include(urpc => urpc.Role)
                                                                   .Include(urpc => urpc.ProfitCenter)
                                                                   .Where(urpc => urpc.Role.RoleEnum == RoleEnum.Admin
                                                                               && urpc.UserId == CurrentUser.Id)
                                                                   .Select(urpc => urpc.ProfitCenter)
                                                                   .ToListAsync())
                                                           .Distinct(new IdPropertyComparer<ProfitCenter>()))
            {
                ModelToReturn.AuthorizedProfitCenterList.Add(new AuthorizedProfitCenterModel(AuthorizedProfitCenter));
            }

            ModelToReturn.SystemDefaultWelcomeEmailText = SystemDefaultWelcomeEmailTextArg;

            return ModelToReturn;
        }

    }

    public class AuthorizedProfitCenterModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public AuthorizedProfitCenterModel(ProfitCenter Arg)
        {
            Id = Arg.Id;
            Name = Arg.Name;
            Code = Arg.ProfitCenterCode;
        }
    }

}
