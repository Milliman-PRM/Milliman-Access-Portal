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
        /// Returns a collection of HostedContentViewModel representing SelectionGroup instances assigned to the specified user
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public virtual List<HostedContentViewModel> GetAssignedUserGroups(string UserName)
        {
            List<HostedContentViewModel> ReturnList = new List<HostedContentViewModel>();
            Dictionary<long, HostedContentViewModel> ResultBuilder = new Dictionary<long, HostedContentViewModel>();

            // Get a list of all content item groups authorized for user, converted to type HostedContentViewModel plus content related properties
            List<HostedContentViewModel> query = DbContext.UserInSelectionGroup
                .Include(usg => usg.User)
                .Include(usg => usg.SelectionGroup)
                    .ThenInclude(sg => sg.RootContentItem)
                        .ThenInclude(rci => rci.Client)
                .Where(usg => usg.User.UserName == UserName)
                .Distinct()
                .Select(usg =>
                    new HostedContentViewModel
                    {
                        UserGroupId = usg.SelectionGroup.Id,
                        ContentName = usg.SelectionGroup.RootContentItem.ContentName,
                        Url = usg.SelectionGroup.ContentInstanceUrl,
                        ClientList = new List<HostedContentViewModel.ParentClientTree>
                        {
                            new HostedContentViewModel.ParentClientTree
                            {
                                Id = usg.SelectionGroup.RootContentItem.ClientId,
                                Name = usg.SelectionGroup.RootContentItem.Client.Name,
                                ParentId = usg.SelectionGroup.RootContentItem.Client.ParentClientId,
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
                        Parent = DbContext.Client
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
