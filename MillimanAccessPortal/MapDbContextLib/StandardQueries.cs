using System;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace MapDbContextLib
{
    public class StandardQueries
    {
        private IServiceScope ServiceScope = null;

        public StandardQueries(IServiceProvider SvcProvider)
        {
            ServiceScope = SvcProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        public List<ContentItemUserGroup> GetAuthorizedUserGroups(string UserName, string[] RoleNamesAuthorizedToViewContent = null)
        {
            List<ContentItemUserGroup> ReturnList = new List<ContentItemUserGroup>();
            // Initialize if no roles provided
            if (RoleNamesAuthorizedToViewContent == null)
            {
                RoleNamesAuthorizedToViewContent = new string[] { "Content User" };
            }

            using (var DataContext = ServiceScope.ServiceProvider.GetService<Context.ApplicationDbContext>())
            {
                // Get all ContentItemUserGroups that the current user is authorized to
                IQueryable<ContentItemUserGroup> AuthorizedGroupQuery = DataContext.ApplicationUser
                    .Where(u => u.UserName == UserName)
                    .Join(DataContext.UserRoleForContentItemUserGroup, au => au.Id, map => map.UserId, (au, map) => map)  // result is found UserRoleForContentItemUserGroup records
                    .Join(DataContext.ApplicationRole, urc => urc.RoleId, r => r.Id, (urc, r) => new { urc = urc, roleName = r.Name })
                    .Where(o => RoleNamesAuthorizedToViewContent.Contains(o.roleName))
                    .Join(DataContext.ContentItemUserGroup, m => m.urc.ContentItemUserGroupId, g => g.Id, (m, g) => g);  // result is found ContentItemUserGroup records

                ReturnList = AuthorizedGroupQuery.ToList();
            }

            return ReturnList;
        }
    }
}
