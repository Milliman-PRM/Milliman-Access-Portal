/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Frequently used database queries for use by the application rather than using messy code in controllers
 * DEVELOPER NOTES: Might be better in the main project, depends on whether the queries tend to be useful to only one application -TP
 */

using System;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.Models.HostedContentViewModels;

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
        /// <param name="RoleNamesAuthorizedToViewContent">Defaults to "Content User"</param>
        /// <returns></returns>
        public List<HostedContentViewModel> GetAuthorizedUserGroupsAndRoles(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                // Get all ContentItemUserGroups that the current user is authorized to
                foreach (var Finding in DataContext.ApplicationUser
                    .Where(u => u.UserName == UserName)
                    .Join(DataContext.UserRoleForContentItemUserGroup, u => u.Id, map => map.UserId, (u, map) => map)  // result is found UserRoleForContentItemUserGroup records
                    .Join(DataContext.ApplicationRole, map => map.RoleId, r => r.Id, (map, r) => new { map = map, role = r })
                    .Join(DataContext.ContentItemUserGroup, prev => prev.map.ContentItemUserGroupId, grp => grp.Id, (prev, grp) =>
                        new
                        {
                            group = grp,
                            urg = prev.map,
                            role = prev.role,
                        })
                    .Join(DataContext.RootContentItem, p => p.group.RootContentItemId, rci => rci.Id, (p, rci) =>
                        new HostedContentViewModel
                        {
                            UserGroupId = p.group.Id,
                            ContentName = rci.ContentName,
                            RoleNames = new HashSet<string>(new string[] { p.role.Name }),
                            Url = p.group.ContentInstanceUrl,
                        }))
                {
                    if (!ResultBuilder.Keys.Contains(Finding.UserGroupId))
                    {
                        // first role for this user/group
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
                var ShortList = DataContext.ContentItemUserGroup
                .Join(DataContext.UserRoleForContentItemUserGroup, g => g.Id, ur => ur.ContentItemUserGroupId, (g, urmap) => new { Group = g, RoleMap = urmap })
                .Join(DataContext.ApplicationUser, prev => prev.RoleMap.UserId, u => u.Id, (prev, u) => new { Group = prev.Group, RoleMap = prev.RoleMap, AppUser = u })
                .Where(u => u.AppUser.UserName == UserName)
                .Where(g => g.Group.Id == GroupId)
                .Where(r => RequiredRoles.Contains(r.RoleMap.Role.RoleEnum))
                .ToList();
                // result is the user's records related to the requested group, filtered to those authorized roles in the list of required roles

                bool AllRequiredRolesFound = RequiredRoles.All(rr => ShortList.Select(s => s.RoleMap.Role.RoleEnum).Contains(rr));

                return AllRequiredRolesFound ? ShortList.Select(s => s.Group).FirstOrDefault() : null;
            }
        }

        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            using (var DataContext = ServiceScope.ServiceProvider.GetService<ApplicationDbContext>())
            {
                IQueryable<Client> AuthorizedClients =
                    DataContext.UserRoleForClient
                    .Where(urc => urc.Role.Name == "Client Administrator")
                    .Where(urc => urc.User.UserName == UserName)
                    .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

                ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here
            }

            return ListOfAuthorizedClients;
        }

    }
}
