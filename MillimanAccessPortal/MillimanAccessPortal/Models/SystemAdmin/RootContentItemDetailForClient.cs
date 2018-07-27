using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentDetailForClient
    {
        public long Id { get; set; }
        public string ContentName { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        public Dictionary<string, List<string>> SelectionGroups { get; set; } = null;

        public static explicit operator RootContentDetailForClient(RootContentItem item)
        {
            if (item == null)
            {
                return null;
            }

            return new RootContentDetailForClient
            {
                Id = item.Id,
                ContentName = item.ContentName,
                ContentType = item.ContentType?.Name,
                Description = item.Description,
                LastUpdated = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
            };
        }
    }
}
