/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES:
 */

using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientAdminViewModels;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DbContext = null;
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
            DbContext = ContextArg;
            UserManager = UserManagerArg;
        }

        /// <summary>
        /// Returns a list of the Clients to which the user is assigned Admin role
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClientsQuery =
                DbContext.UserRoleInClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DbContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClientsQuery);  // Query executes here

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

            List<Client> FoundChildClients = DbContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();

            ReturnList.AddRange(FoundChildClients);
            if (Recurse)
            {
                // Get grandchildren too
                foreach (Client ChildClient in FoundChildClients)
                {
                    ReturnList.AddRange(GetChildClients(ChildClient, Recurse));
                }
            }

            return ReturnList;
        }

        public Client GetRootClientOfClient(long id)
        {
            // Execute query here so there is only one db query and the rest is done locally in memory
            List<Client> AllClients = DbContext.Client.ToList();

            // start with the client id supplied
            Client CandidateResult = AllClients.SingleOrDefault(c => c.Id == id);

            // search up the parent hierarchy
            while (CandidateResult != null && CandidateResult.ParentClientId != null)
            {
                CandidateResult = AllClients.SingleOrDefault(c => c.Id == CandidateResult.ParentClientId);
            }

            return CandidateResult;
        }

        public List<Client> GetAllRootClients()
        {
            return DbContext.Client.Where(c => c.ParentClientId == null).ToList();
        }

        /// <summary>
        /// Returns list of normalized role names authorized to provided Client for provided UserId
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="ClientId"></param>
        /// <returns></returns>
        public List<AssignedRoleInfo> GetUserRolesForClient(long UserId, long ClientId)
        {
            IQueryable<AssignedRoleInfo> Query = DbContext.UserRoleInClient
                                                            .Include(urc => urc.Role)
                                                            .Where(urc => urc.UserId == UserId
                                                                       && urc.ClientId == ClientId)
                                                            .Distinct()
                                                            .Select(urc =>
                                                                new AssignedRoleInfo
                                                                {
                                                                    RoleEnum = urc.Role.RoleEnum,
                                                                    RoleDisplayValue = ApplicationRole.RoleDisplayNames[urc.Role.RoleEnum],
                                                                    IsAssigned = true,
                                                                });

            List<AssignedRoleInfo> ReturnVal = Query.ToList();

            return ReturnVal;
        }

        /// <summary>
        /// Returns an ApplicationUser entity associated with the provided ClaimsPrincipal, using an injected UserManager
        /// TODO: If this method is only called from controllers where injected services are available, consider removing 
        /// this and calling the UserManager directly from all referring code.  This was intended to help make the user 
        /// object available from places where the UserManager was not easily accessible (injected services not available).  
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        internal async Task<ApplicationUser> GetCurrentApplicationUser(ClaimsPrincipal User)
        {
            return await UserManager.GetUserAsync(User);
        }

    }
}
