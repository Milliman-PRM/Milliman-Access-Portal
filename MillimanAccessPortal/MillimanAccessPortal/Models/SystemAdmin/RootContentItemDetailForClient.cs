/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide root content item information for display in the system admin detail panel
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
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
        public bool IsPublishing { get; set; }
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
            IsPublishing = dbContext.ContentPublicationRequest
                .Where(pr => pr.RootContentItemId == Id)
                .Where(pr => pr.RequestStatus.IsActive())
                .Any();

            var groups = dbContext.UserInSelectionGroup
                .Where(g => g.SelectionGroup.RootContentItemId == Id)
                .Where(g => g.SelectionGroup.RootContentItem.ClientId == clientId)
                .Include(g => g.SelectionGroup)
                .Include(g => g.User)
                .ToList();

            var selectionGroups = new NestedList();
            foreach (var group in groups)
            {
                if (!selectionGroups.Sections.Any(s => s.Name == group.SelectionGroup.GroupName))
                {
                    selectionGroups.Sections.Add(new NestedListSection
                    {
                        Name = group.SelectionGroup.GroupName,
                        Id = group.SelectionGroup.Id,
                        Marked =  dbContext.ContentReductionTask
                            .Where(rt => rt.SelectionGroupId == group.SelectionGroup.Id)
                            .Where(rt => rt.ReductionStatus.IsActive())
                            .Where(rt => rt.ContentPublicationRequestId == null)
                            .Any(),
                    });
                }
                selectionGroups
                    .Sections.Single(s => s.Name == group.SelectionGroup.GroupName)
                    .Values.Add($"{group.User.FirstName} {group.User.LastName}");
            }

            SelectionGroups = selectionGroups;
        }
    }
}
