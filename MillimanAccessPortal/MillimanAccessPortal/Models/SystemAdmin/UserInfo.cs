/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<RootContentItemInfo> RootContentItems { get; set; }

        public static explicit operator UserInfo(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserInfo
            {
                Id = user.Id,
                Name = user.FirstName,
            };
        }

        public void IncludeRootContentItems(ApplicationDbContext dbContext)
        {
            var query = dbContext.UserInSelectionGroup
                .Where(usg => usg.UserId == Id)
                .Select(usg => usg.SelectionGroup.RootContentItem);

            var itemInfoList = new List<RootContentItemInfo>();
            foreach (var item in query)
            {
                var itemInfo = (RootContentItemInfo)item;
                itemInfoList.Add(itemInfo);
            }

            RootContentItems = itemInfoList;
        }
    }
}
