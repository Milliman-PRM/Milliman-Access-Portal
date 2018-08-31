/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide root content item information for presentation on a root content item card.
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ClientName { get; set; }
        public int? UserCount { get; set; }
        public int? SelectionGroupCount { get; set; }
        public List<UserInfo> Users { get; set; }
        public bool IsSuspended { get; set; }

        public static explicit operator RootContentItemInfo(RootContentItem rootContentItem)
        {
            if (rootContentItem == null)
            {
                return null;
            }

            return new RootContentItemInfo
            {
                Id = rootContentItem.Id,
                Name = rootContentItem.ContentName,
                IsSuspended = rootContentItem.IsSuspended,
            };
        }

        public void QueryRelatedEntityCounts(ApplicationDbContext dbContext, Guid? userId)
        {
            ClientName = dbContext.RootContentItem
                .Where(i => i.Id == Id)
                .Select(i => i.Client.Name)
                .Single();

            if (userId.HasValue)
            {
                // don't count users
                
                // don't count selection groups
            }
            else
            {
                // count all users and selection groups related to the root content item
                UserCount = dbContext.UserInSelectionGroup
                    .Where(usg => usg.SelectionGroup.RootContentItemId == Id)
                    .Count();

                SelectionGroupCount = dbContext.SelectionGroup
                    .Where(group => group.RootContentItemId == Id)
                    .Count();

                _includeUsers(dbContext);
            }
        }

        private void _includeUsers(ApplicationDbContext dbContext)
        {
            var query = dbContext.UserInSelectionGroup
                .Where(usg => usg.SelectionGroup.RootContentItemId == Id)
                .Select(usg => usg.User);

            var userInfoList = new List<UserInfo>();
            foreach (var user in query)
            {
                var userInfo = (UserInfo)user;
                userInfoList.Add(userInfo);
            }

            Users = userInfoList;
        }
    }
}
