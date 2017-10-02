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

namespace MillimanAccessPortal
{
    public class StandardQueries
    {
        private IServiceScope ServiceScope = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(IServiceProvider SvcProvider)
        {
            ServiceScope = SvcProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
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

            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
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
            }

            ResultBuilder.ToList().ForEach(h => ReturnList.Add(h.Value));

            return ReturnList.ToList();
        }

        /// <summary>
        /// determines whether the supplied user name is authorized to the supplied group for all supplied role names
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="GroupId"></param>
        /// <param name="RequiredRoleArray"></param>
        /// <returns>true iff user is authorized to the group in all roles</returns>
        public  bool IsUserAuthorizedToAllRolesForGroup(string UserName, long GroupId, IEnumerable<RoleEnum> RequiredRoles)
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
            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                var ShortList = DataContext.UserRoleForContentItemUserGroup
                    .Include(urg => urg.Role)
                    .Include(urg => urg.User)
                    .Include(urg => urg.ContentItemUserGroup)
                    .Where(urg => urg.ContentItemUserGroupId == GroupId)
                    .Where(urg => urg.User.UserName == UserName)
                    .Where(urg => RequiredRoles.Contains(urg.Role.RoleEnum))
                    .ToList();
                // result is the user's authorizations for the requested group, filtered to only roles in the caller provided list of required roles

                bool AllRequiredRolesFound = RequiredRoles.All(rr => ShortList.Select(urg => urg.Role.RoleEnum).Contains(rr));

                return AllRequiredRolesFound ? ShortList.Select(urg => urg.ContentItemUserGroup).FirstOrDefault() : null;
            }
        }

        public ContentItemUserGroup GetUserGroupIfAuthorizedToRole(string UserName, long GroupId, RoleEnum RequiredRole)
        {
            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                var ShortList = DataContext.UserRoleForContentItemUserGroup
                    .Include(urg => urg.Role)
                    .Include(urg => urg.User)
                    .Include(urg => urg.ContentItemUserGroup)
                    .Where(urg => urg.ContentItemUserGroupId == GroupId)
                    .Where(urg => urg.User.UserName == UserName)
                    .Where(urg => urg.Role.RoleEnum == RequiredRole)
                    .Select(s => s.ContentItemUserGroup);

                return ShortList.FirstOrDefault();
            }
        }

        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                IQueryable<Client> AuthorizedClients =
                    DataContext.UserRoleForClient
                    .Where(urc => urc.Role.RoleEnum == RoleEnum.ClientAdministrator)
                    .Where(urc => urc.User.UserName == UserName)
                    .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

                ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here
            }

            return ListOfAuthorizedClients;
        }

    }
}
