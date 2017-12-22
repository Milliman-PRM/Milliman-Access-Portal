/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES: 
 */

using System.Threading.Tasks;
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
        /// Returns a list of the Clients to which the user is assigned Admin role
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClientsQuery =
                DataContext.UserRoleInClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

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

            List<Client> FoundChildClients = DataContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();

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
            List<Client> AllClients = DataContext.Client.ToList();

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
            return DataContext.Client.Where(c => c.ParentClientId == null).ToList();
        }

        public async Task<ClientAndChildrenModel> GetDescendentFamilyOfClient(Client ClientArg, ApplicationUser CurrentUser, RoleEnum ClientRoleRequiredToManage, bool RequireProfitCenterAuthority, bool RecurseDown = true)
        {
            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientArg.Id.ToString());
            List<ApplicationUser> UserMembersOfThisClient = (await UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim)).ToList();

            ClientAndChildrenModel ResultObject = new ClientAndChildrenModel { ClientEntity = ClientArg };  // Initialize.
            ResultObject.AssociatedContentCount = DataContext.RootContentItem.Where(r => r.ClientIdList.Contains(ClientArg.Id)).Count();
            ResultObject.AssociatedUserCount = UserMembersOfThisClient.Count;

            ResultObject.CanManage = DataContext.UserRoleInClient
                                                .Include(urc => urc.Role)
                                                .Include(urc => urc.Client)
                                                .Any(urc => urc.UserId == CurrentUser.Id
                                                         && urc.Role.RoleEnum == ClientRoleRequiredToManage
                                                         && urc.ClientId == ClientArg.Id);

            if (RequireProfitCenterAuthority)
            {
                ResultObject.CanManage &= DataContext.UserRoleInProfitCenter
                                                     .Include(urp => urp.Role)
                                                     .Any(urp => urp.UserId == CurrentUser.Id
                                                              && urp.Role.RoleEnum == RoleEnum.Admin
                                                              && urp.ProfitCenterId == ClientArg.ProfitCenterId);
            }

            if (RecurseDown)
            {
                List<Client> ChildrenOfThisClient = DataContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();
                foreach (Client ChildOfThisClient in ChildrenOfThisClient)
                {
                    ResultObject.Children.Add(await GetDescendentFamilyOfClient(ChildOfThisClient, CurrentUser, ClientRoleRequiredToManage, RecurseDown));
                }
            }

            return ResultObject;
        }

        /// <summary>
        /// Returns list of normalized role names authorized to provided Client for provided UserId
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name="ClientId"></param>
        /// <returns></returns>
        public List<string> GetUserRolesForClient(long UserId, long ClientId)
        {
            List<string> ReturnVal = DataContext
                                    .UserRoleInClient
                                    .Include(urc => urc.Role)
                                    .Where(urc => urc.UserId == UserId
                                               && urc.ClientId == ClientId)
                                    .Select(urc => urc.Role.NormalizedName)
                                    .Distinct()
                                    .ToList();

            return ReturnVal;
        }

        internal async Task<ApplicationUser> GetCurrentApplicationUser(ClaimsPrincipal User)
        {
            return await UserManager.GetUserAsync(User);
        }


    }
}
