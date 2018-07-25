/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<UserInfo> Users { get; set; }

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
            };
        }

        public void IncludeUsers(ApplicationDbContext dbContext)
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
