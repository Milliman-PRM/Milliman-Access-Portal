/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Frequently used database queries for use by the application rather than using messy code in controllers
 * DEVELOPER NOTES: Might be better in the main project, depends on whether the queries tend to be useful to only one application -TP
 */

using System;
using System.Collections.Generic;
using System.Text;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.Models.HostedContentViewModels;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.ClientAdminViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;

namespace MillimanAccessPortal
{
    public class StandardQueries
    {
        private IServiceScope ServiceScope = null;
        private UserManager<ApplicationUser> UserManager = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(IServiceProvider SvcProvider)
        {
            ServiceScope = SvcProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            UserManager = ServiceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        ~StandardQueries()
        {
            ServiceScope.Dispose();
        }

        /// <summary>
        /// Returns the collection of ContentItemUserGroup instances authorized to the specified user in the specified roles
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public List<HostedContentViewModel> GetAuthorizedUserGroupsAndRoles(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

            var query = DataContext.UserRoleForContentItemUserGroup
                .Include(urg => urg.User)
                .Include(urg => urg.Role)
                .Include(urg => urg.ContentItemUserGroup)
                    .ThenInclude(ug => ug.RootContentItem)
                .Include(urg => urg.ContentItemUserGroup)
                    .ThenInclude(ug => ug.Client)
                .Where(urg => urg.User.UserName == UserName)
                .Select(urg => 
                    new HostedContentViewModel
                    {
                        UserGroupId = urg.ContentItemUserGroup.Id,
                        ContentName = urg.ContentItemUserGroup.RootContentItem.ContentName,
                        RoleNames = new HashSet<string>(new string[] { urg.Role.Name }),
                        Url = urg.ContentItemUserGroup.ContentInstanceUrl,
                        ClientList = new List<HostedContentViewModel.ParentClientTree>
                        {
                            new HostedContentViewModel.ParentClientTree
                            {
                                Id = urg.ContentItemUserGroup.ClientId,
                                Name = urg.ContentItemUserGroup.Client.Name,
                                ParentId = urg.ContentItemUserGroup.Client.ParentClientId,
                            }
                        },
                    }
                ).ToList();

            foreach (var Finding in query)
            {
                if (!ResultBuilder.Keys.Contains(Finding.UserGroupId))
                {
                    // Build the list of parent client hierarchy for Finding
                    while (Finding.ClientList.First().ParentId != null)
                    {
                        Client Parent = null;
                        try
                        {
                            Parent = DataContext.Client
                                .Where(c => c.Id == Finding.ClientList.First().ParentId)
                                .First();  // will throw if not found but that's good
                        }
                        catch (Exception e)
                        {
                            throw new MapException($"Client record references parent id {Finding.ClientList.Last().ParentId} but an exception occurred while querying for this Client", e);
                        }

                        // The required order is root down to 
                        Finding.ClientList.Insert(0,
                            new HostedContentViewModel.ParentClientTree
                            {
                                Id = Parent.Id,
                                Name = Parent.Name,
                                ParentId = Parent.ParentClientId,
                            }
                        );
                    }

                    ResultBuilder.Add(Finding.UserGroupId, Finding);
                }
                else
                {
                    // additional role for this user/group
                    ResultBuilder[Finding.UserGroupId].RoleNames.Add(Finding.RoleNames.First());
                }
            }

            ResultBuilder.ToList().ForEach(h => ReturnList.Add(h.Value));

            return ReturnList.ToList();
        }

        public List<Client> GetAllRelatedClients(Client ClientArg)
        {
            List<Client> ReturnList = new List<Client>();

            Client RootClient = GetRootClientOfClient(ClientArg.Id);
            ReturnList.Add(RootClient);
            ReturnList.AddRange(GetChildClients(RootClient, true));

            return ReturnList;
        }

        public List<Client> GetAllRootClients()
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();
            return DataContext.Client.Where(c => c.ParentClientId == null).ToList();
        }

        public ClientAndChildrenModel GetDescendentFamilyOfClient(Client ClientArg, long CurrentUserId, bool RecurseDown=true)
        {
            ApplicationDbContext DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

            Claim ThisClientMembershipClaim = new Claim(ClaimNames.ClientMembership.ToString(), ClientArg.Name);
            List<ApplicationUser> UserMembersOfThisClient = UserManager.GetUsersForClaimAsync(ThisClientMembershipClaim).Result.ToList();

            ClientAndChildrenModel ResultObject = new ClientAndChildrenModel { ClientEntity = ClientArg };  // Initialize.  Relies on implicit conversion operator
            ResultObject.AssociatedContentCount = DataContext.RootContentItem.Where(r => r.ClientIdList.Contains(ClientArg.Id)).Count();
            ResultObject.AssociatedUserCount = UserMembersOfThisClient.Count;
            ResultObject.CanManage = DataContext
                                        .UserRoleForClient
                                        .Include(URCMap => URCMap.Role)
                                        .Include(URCMap => URCMap.User)
                                        .SingleOrDefault(URCMap => URCMap.UserId == CurrentUserId
                                                                && URCMap.Role.RoleEnum == RoleEnum.ClientAdministrator
                                                                && URCMap.ClientId == ClientArg.Id)
                                        != null;

            if (RecurseDown)
            {
                List<Client> ChildrenOfThisClient = DataContext.Client.Where(c => c.ParentClientId == ClientArg.Id).ToList();
                foreach (Client C in ChildrenOfThisClient)
                {
                    ResultObject.Children.Add(GetDescendentFamilyOfClient(C, CurrentUserId, RecurseDown));
                }
            }

            return ResultObject;
        }

        public Client GetRootClientOfClient(long id)
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

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

        private List<Client> GetChildClients(Client ClientArg, bool Recurse=false)
        {
            List<Client> ReturnList = new List<Client>();

            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();
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

/*        public ClientAndChildrenViewModel GetRootClientViewModelOfClient(long ClientIdArg)
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();
            List<Client> AllClients = DataContext.Client.ToList();

            Client NextParent = AllClients.SingleOrDefault(c => c.Id == ClientIdArg);

            while (NextParent != null && NextParent.ParentClientId != null)
            {
                NextParent = AllClients.SingleOrDefault(c => c.Id == NextParent.ParentClientId);
            }

            return NextParent;
        }*/



        /// <summary>
        /// determines whether the supplied user name is authorized to the supplied group for all supplied role names
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="GroupId"></param>
        /// <param name="RequiredRoleArray"></param>
        /// <returns>true iff user is authorized to the group in all roles</returns>
        public bool IsUserAuthorizedToAllRolesForGroup(string UserName, long GroupId, IEnumerable<RoleEnum> RequiredRoles)
        {
            var AuthorizedGroupForUser = GetUserGroupIfAuthorizedToAllRoles(UserName, GroupId, RequiredRoles);

            return AuthorizedGroupForUser != null;
        }

        /// <summary>
        /// Tests whether the requested group is authorized to user for all specified roles
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="GroupId"></param>
        /// <param name="RequiredRoles"></param>
        /// <returns></returns>
        public ContentItemUserGroup GetUserGroupIfAuthorizedToAllRoles(string UserName, long GroupId, IEnumerable<RoleEnum> RequiredRoles)
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

            var ShortList = DataContext
                .UserRoleForContentItemUserGroup
                .Include(urg => urg.Role)
                .Include(urg => urg.User)
                .Include(urg => urg.ContentItemUserGroup)
                .Where(urg => urg.ContentItemUserGroupId == GroupId 
                           && urg.User.UserName == UserName
                           && RequiredRoles.Contains(urg.Role.RoleEnum))
                .ToList();
            // result is the user's authorizations for the requested group, filtered to only roles in the caller provided list of required roles

            bool AllRequiredRolesFound = RequiredRoles.All(rr => ShortList.Select(urg => urg.Role.RoleEnum).Contains(rr));

            return AllRequiredRolesFound ? ShortList.Select(urg => urg.ContentItemUserGroup).FirstOrDefault() : null;
        }

        public ContentItemUserGroup GetUserGroupIfAuthorizedToRole(string UserName, long GroupId, RoleEnum RequiredRole)
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

            var ShortList = DataContext
                .UserRoleForContentItemUserGroup
                .Include(urg => urg.Role)
                .Include(urg => urg.User)
                .Include(urg => urg.ContentItemUserGroup)
                .Where(urg => urg.ContentItemUserGroupId == GroupId)
                .Where(urg => urg.User.UserName == UserName)
                .Where(urg => urg.Role.RoleEnum == RequiredRole)
                .Select(s => s.ContentItemUserGroup);

            return ShortList.FirstOrDefault();
        }

        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

            IQueryable<Client> AuthorizedClients = DataContext
                .UserRoleForClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ClientAdministrator
                           && urc.User.UserName == UserName)
                .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here

            return ListOfAuthorizedClients;
        }

        public List<string> GetUserRolesForClient(long UserId, long ClientId)
        {
            var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>();

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
