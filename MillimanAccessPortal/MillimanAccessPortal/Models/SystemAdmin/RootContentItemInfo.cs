/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }

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
    }
}
