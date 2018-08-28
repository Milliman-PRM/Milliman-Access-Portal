using System;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemDetailForUser
    {
        public Guid Id { get; set; }
        public string ContentName { get; set; }
        public string ContentType { get; set; }

        public static explicit operator RootContentItemDetailForUser(RootContentItem item)
        {
            if (item == null)
            {
                return null;
            }

            return new RootContentItemDetailForUser
            {
                Id = item.Id,
                ContentName = item.ContentName,
                ContentType = item.ContentType?.Name,
            };
        }
    }
}
