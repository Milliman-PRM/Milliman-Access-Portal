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
        /// Returns a collection of HostedContentViewModel representing ContentItemUserGroup instances assigned to the specified user
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public virtual List<HostedContentViewModel> GetAssignedUserGroups(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

            // Get a list of all content item groups authorized for user, converted to type HostedContentViewModel plus content related properties
            List<HostedContentViewModel> query = DataContext.UserInContentItemUserGroup
                .Include(ug => ug.User)
                .Include(ug => ug.ContentItemUserGroup)
                    .ThenInclude(ug => ug.RootContentItem)
                .Include(ug => ug.ContentItemUserGroup)
                    .ThenInclude(ug => ug.Client)
                .Where(ug => ug.User.UserName == UserName)
                .Distinct()
                .Select(ug =>
                    new HostedContentViewModel
                    {
                        UserGroupId = ug.ContentItemUserGroup.Id,
                        ContentName = ug.ContentItemUserGroup.RootContentItem.ContentName,
                        Url = ug.ContentItemUserGroup.ContentInstanceUrl,
                        ClientList = new List<HostedContentViewModel.ParentClientTree>
                        {
                            new HostedContentViewModel.ParentClientTree
                            {
                                Id = ug.ContentItemUserGroup.ClientId,
                                Name = ug.ContentItemUserGroup.Client.Name,
                                ParentId = ug.ContentItemUserGroup.Client.ParentClientId,
                            }
                        },
                    })
                .ToList();

            foreach (var Finding in query)
            {
                // Build the list of parent client hierarchy for Finding
                while (Finding.ClientList.First().ParentId != null)
                {
                    Client Parent = null;
                    try
                    {
                        // Verify that the referenced parent exists
                        Parent = DataContext.Client
                            .Where(c => c.Id == Finding.ClientList.First().ParentId)
                            .First();  // will throw if not found but that's what I'm checking
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

            ResultBuilder.ToList().ForEach(h => ReturnList.Add(h.Value));

            return ReturnList.ToList();
        }

    }
}
