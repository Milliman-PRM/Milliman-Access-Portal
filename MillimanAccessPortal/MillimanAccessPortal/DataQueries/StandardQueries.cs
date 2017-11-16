/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES: 
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientAdminViewModels;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DataContext = null;
        private UserManager<ApplicationUser> UserManager = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(
            ApplicationDbContext ContextArg,
            UserManager<ApplicationUser> UserManagerArg
            )
        {
            DataContext = ContextArg;
            UserManager = UserManagerArg;
        }

        /// <summary>
        /// Returns a list of the Clients to which the user is assigned ClientAdministrator role
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClients =
                DataContext.UserRoleForClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ClientAdmin)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here

            return ListOfAuthorizedClients;
        }

        public List<Client> GetAllRelatedClients(Client ClientArg)
        {
            List<Client> ReturnList = new List<Client>();

            Client RootClient = GetRootClientOfClient(ClientArg.Id);
            ReturnList.Add(RootClient);
            ReturnList.AddRange(GetChildClients(RootClient, true));

            return ReturnList;
        }

        private List<Client> GetChildClients(Client ClientArg, bool Recurse = false)
        {
            List<Client> ReturnList = new List<Client>();

            List<Client> ThisLevelClients = DataContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();

            ReturnList.AddRange(ThisLevelClients);
            if (Recurse)
            {
                foreach (Client OneChild in ThisLevelClients)
                {
                    ReturnList.AddRange(GetChildClients(OneChild, Recurse));
                }
            }

            return ReturnList;
        }

        public Client GetRootClientOfClient(long id)
        {
            // Do this so there is only one db query and the rest is done locally in memory
            List<Client> AllClients = DataContext.Client.ToList();

            // start with the client id supplied
            Client NextParent = AllClients.SingleOrDefault(c => c.Id == id);

            // search up the parent hierarchy
            while (NextParent != null && NextParent.ParentClientId != null)
            {
                NextParent = AllClients.SingleOrDefault(c => c.Id == NextParent.ParentClientId);
            }

            return NextParent;
        }

        public List<Client> GetAllRootClients()
        {
            return DataContext.Client.Where(c => c.ParentClientId == null).ToList();
        }

        public ClientAndChildrenModel GetDescendentFamilyOfClient(Client ClientArg, ApplicationUser CurrentUser, bool RecurseDown = true)
        {
            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientArg.Id.ToString());
            List<ApplicationUser> UserMembersOfThisClient = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result.ToList();

            ClientAndChildrenModel ResultObject = new ClientAndChildrenModel { ClientEntity = ClientArg };  // Initialize.
            ResultObject.AssociatedContentCount = DataContext.RootContentItem.Where(r => r.ClientIdList.Contains(ClientArg.Id)).Count();
            ResultObject.AssociatedUserCount = UserMembersOfThisClient.Count;
            ResultObject.CanManage = DataContext.UserRoleForClient
                                                .Include(URCMap => URCMap.Role)
                                                .Include(URCMap => URCMap.User)
                                                .Join(DataContext.UserClaims, URCMap => URCMap.UserId, claim => claim.UserId, (URCMap, claim) => new { URCMap = URCMap, Claim = claim })
                                                .SingleOrDefault(rec => rec.URCMap.UserId == CurrentUser.Id
                                                                     && rec.URCMap.Role.RoleEnum == RoleEnum.ClientAdmin
                                                                     && rec.URCMap.ClientId == ClientArg.Id
                                                                     // verify that the user has a claim of ProfitCenterManager to the ProfitCenter of the client
                                                                     && rec.Claim.ClaimType == ClaimNames.ProfitCenterManager.ToString()
                                                                     && rec.Claim.ClaimValue == ClientArg.ProfitCenterId.ToString())
                                                != null;

            if (RecurseDown)
            {
                List<Client> ChildrenOfThisClient = DataContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();
                foreach (Client C in ChildrenOfThisClient)
                {
                    ResultObject.Children.Add(GetDescendentFamilyOfClient(C, CurrentUser, RecurseDown));
                }
            }

            return ResultObject;
        }

        public List<string> GetUserRolesForClient(long UserId, long ClientId)
        {
            List<string> ReturnVal = DataContext
                                    .UserRoleForClient
                                    .Include(urc => urc.Role)
                                    .Where(urc => urc.UserId == UserId
                                               && urc.ClientId == ClientId)
                                    .Select(urc => urc.Role.NormalizedName)
                                    .Distinct()
                                    .ToList();

            return ReturnVal;
        }

    }
}
