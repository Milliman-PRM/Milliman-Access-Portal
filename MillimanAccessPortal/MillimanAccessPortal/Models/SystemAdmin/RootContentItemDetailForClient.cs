using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public void QueryRelatedEntities(ApplicationDbContext dbContext, long clientId)
        {
            var groups = dbContext.UserInSelectionGroup
                .Where(g => g.SelectionGroup.RootContentItemId == Id)
                .Where(g => g.SelectionGroup.RootContentItem.ClientId == clientId)
                .Select(g => new KeyValuePair<string, string>(g.SelectionGroup.GroupName, $"{g.User.FirstName} {g.User.LastName}"));

            var selectionGroups = new Dictionary<string, List<string>>();
            foreach (var group in groups)
            {
                if (!selectionGroups.Keys.Contains(group.Key))
                {
                    selectionGroups[group.Key] = new List<string>();
                }
                selectionGroups[group.Key].Add(group.Value);
            }

            SelectionGroups = selectionGroups;
        }
    }
}
