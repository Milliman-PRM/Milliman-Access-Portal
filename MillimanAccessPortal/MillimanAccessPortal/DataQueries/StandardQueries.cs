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
using AuditLogLib.Services;
using AuditLogLib.Event;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DbContext = null;
        private UserManager<ApplicationUser> _userManager = null;
        private IAuditLogger _auditLog = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(
            ApplicationDbContext ContextArg,
            UserManager<ApplicationUser> UserManagerArg,
            IAuditLogger AuditLogArg
            )
        {
            DbContext = ContextArg;
            _userManager = UserManagerArg;
            _auditLog = AuditLogArg;
        }

        /// <summary>
        /// Creates a new user account and records to the audit log if successful
        /// </summary>
        /// <param name="UserNameArg"></param>
        /// <param name="EmailArg"></param>
        /// <returns>On success, returns the new ApplicationUser instance, null otherwise</returns>
        internal async Task<(IdentityResult result, ApplicationUser user)> CreateNewAccount(string UserNameArg, string EmailArg)
        {
            var RequestedUser = new ApplicationUser
            {
                UserName = UserNameArg,
                Email = EmailArg,
            };
            IdentityResult result = await _userManager.CreateAsync(RequestedUser);

            if (result.Succeeded)
            {
                _auditLog.Log(AuditEventType.UserAccountCreated.ToEvent(RequestedUser));
            }
            else
            {
                RequestedUser = null;
            }
            return (result, RequestedUser);
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

        public Client GetRootClientOfClient(Guid id)
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
        public List<AssignedRoleInfo> GetUserRolesForClient(Guid UserId, Guid ClientId)
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
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        internal async Task<ApplicationUser> GetCurrentApplicationUser(ClaimsPrincipal User)
        {
            return await _userManager.GetUserAsync(User);
        }

        public class TrimCaseInsensitiveStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string l, string r)
            {
                if (ReferenceEquals(l, r)) return true;
                if (ReferenceEquals(l, null) || ReferenceEquals(r, null)) return false;
                return l.Trim().ToLower() == r.Trim().ToLower();
            }
            public int GetHashCode(string Arg)
            {
                return Arg.Trim().ToLower().GetHashCode();
            }
        };
        public bool DoesEmailSatisfyClientWhitelists(string email, IEnumerable<string> domains, IEnumerable<string> addresses)
        {
            IEqualityComparer<string> comparer = new TrimCaseInsensitiveStringComparer();

            return domains.Contains(email.Substring(email.IndexOf('@')+1), comparer) || addresses.Contains(email, comparer);
        }
    }
}
