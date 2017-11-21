/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Wrapper for database queries used by HostedContentController.  
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MillimanAccessPortal.Models.HostedContentViewModels;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;
using MapCommonLib;

namespace MillimanAccessPortal.DataQueries
{
    public partial class StandardQueries
    {
        /// <summary>
        /// Returns the collection of ContentItemUserGroup instances authorized to the specified user
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public virtual List<HostedContentViewModel> GetAuthorizedUserGroupsAndRoles(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

            // Get a list of all content item groups authorized for user, converted to type HostedContentViewModel plus content related properties
            List<HostedContentViewModel> query = DataContext.UserRoleForContentItemUserGroup
                .Include(urg => urg.User)
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
                    })
                .ToList();

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
            }

            ResultBuilder.ToList().ForEach(h => ReturnList.Add(h.Value));

            return ReturnList.ToList();
        }

        /// <summary>
        /// Returns the requested ContentItemUserGroup entity object if the supplied user is authorized in the supplied role
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="GroupId"></param>
        /// <param name="RequiredRole"></param>
        /// <returns></returns>
        public ContentItemUserGroup GetUserGroupIfAuthorized(string UserName, long GroupId)
        {
            var ShortList = DataContext.UserRoleForContentItemUserGroup
                .Include(urg => urg.User)
                .Include(urg => urg.ContentItemUserGroup)
                .Where(urg => urg.ContentItemUserGroupId == GroupId)
                .Where(urg => urg.User.UserName == UserName)
                .Select(s => s.ContentItemUserGroup);

            return ShortList.FirstOrDefault();
        }

    }
}
