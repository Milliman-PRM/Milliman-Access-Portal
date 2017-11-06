/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Wrapper for database queries.  Reusable methods appear in this file, methods for single caller appear in files named for the caller
 * DEVELOPER NOTES: 
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

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        private ApplicationDbContext DataContext = null;

        /// <summary>
        /// Constructor, stores local copy of the caller's IServiceScope
        /// </summary>
        /// <param name="SvcProvider"></param>
        public StandardQueries(ApplicationDbContext ContextArg)
        {
            DataContext = ContextArg;
        }

        /// <summary>
        /// Returns the collection of ContentItemUserGroup instances authorized to the specified user in the specified roles
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public virtual List<HostedContentViewModel> GetAuthorizedUserGroupsAndRoles(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

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

        public ContentItemUserGroup GetUserGroupIfAuthorizedToRole(string UserName, long GroupId, RoleEnum RequiredRole)
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

        public List<Client> GetListOfClientsUserIsAuthorizedToManage(string UserName)
        {
            List<Client> ListOfAuthorizedClients = new List<Client>();
            IQueryable<Client> AuthorizedClients =
                DataContext.UserRoleForClient
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ClientAdministrator)
                .Where(urc => urc.User.UserName == UserName)
                .Join(DataContext.Client, urc => urc.ClientId, c => c.Id, (urc, c) => c);

            ListOfAuthorizedClients.AddRange(AuthorizedClients);  // Query executes here

            return ListOfAuthorizedClients;
        }

    }
}
