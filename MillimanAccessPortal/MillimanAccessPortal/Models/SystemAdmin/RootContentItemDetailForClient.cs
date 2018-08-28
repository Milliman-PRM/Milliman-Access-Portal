using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemDetailForClient
    {
        public Guid Id { get; set; }
        public string ContentName { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        public NestedList SelectionGroups { get; set; } = null;

        public static explicit operator RootContentItemDetailForClient(RootContentItem item)
        {
            if (item == null)
            {
                return null;
            }

            return new RootContentItemDetailForClient
            {
                Id = item.Id,
                ContentName = item.ContentName,
                ContentType = item.ContentType?.Name,
                Description = item.Description,
                LastUpdated = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
            };
        }

        public void QueryRelatedEntities(ApplicationDbContext dbContext, Guid clientId)
        {
            var groups = dbContext.UserInSelectionGroup
                .Where(g => g.SelectionGroup.RootContentItemId == Id)
                .Where(g => g.SelectionGroup.RootContentItem.ClientId == clientId)
                .Select(g => new KeyValuePair<string, string>(g.SelectionGroup.GroupName, $"{g.User.FirstName} {g.User.LastName}"));

            var selectionGroups = new NestedList();
            foreach (var group in groups)
            {
                if (!selectionGroups.Sections.Any(s => s.Name == group.Key))
                {
                    selectionGroups.Sections.Add(new NestedListSection
                    {
                        Name = group.Key,
                    });
                }
                selectionGroups.Sections.Single(s => s.Name == group.Key).Values.Add(group.Value);
            }

            SelectionGroups = selectionGroups;
        }
    }
}
