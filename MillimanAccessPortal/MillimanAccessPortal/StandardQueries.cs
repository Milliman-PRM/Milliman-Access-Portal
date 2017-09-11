/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Frequently used database queries for use by the application rather than using messy code in controllers
 * DEVELOPER NOTES: Might be better in the main project, depends on whether the queries tend to be useful to only one application -TP
 */

using System;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.Models.HostedContentViewModels;

namespace MapDbContextLib
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

            using (var DataContext = ServiceScope.ServiceProvider.GetService<Context.ApplicationDbContext>())
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
                        ResultBuilder.Add(Finding.UserGroupId, Finding);
                    }
                    else
                    {
                        // second role for this user/group
                        ResultBuilder[Finding.UserGroupId].RoleNames.Add(Finding.RoleNames.First());
                    }
                }
            }

            ResultBuilder.ToList().ForEach(h => ReturnList.Add(h.Value));

            return ReturnList.ToList();
        }
    }
}
