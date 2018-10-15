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

            // query for selection groups and users in selection groups, then assign to a dictionary
            var selectionGroups = dbContext.SelectionGroup
                .Where(g => g.RootContentItemId == Id)
                .Where(g => g.RootContentItem.ClientId == clientId)
                .ToList();

            var selectionGroupIds = selectionGroups.Select(g => g.Id).ToList();
            var usersInSelectionGroups = dbContext.UserInSelectionGroup
                .Where(g => selectionGroupIds.Contains(g.SelectionGroupId))
                .Include(g => g.User)
                .ToList();

            var selectionGroupUsersDictionary = selectionGroups.ToDictionary(
                g => g.Id,
                g => usersInSelectionGroups
                    .Where(u => u.SelectionGroupId == g.Id)
                    .Select(u => u.User).ToList());

            var selectionGroupList = new NestedList();
            foreach (var groupId in selectionGroupIds)
            {
                var group = selectionGroups.Single(g => g.Id == groupId);
                if (!selectionGroupList.Sections.Any(s => s.Name == group.GroupName))
                {
                    var reductionTasks = dbContext.ContentReductionTask
                        .Where(rt => rt.SelectionGroupId == group.Id)
                        .Where(rt => rt.ReductionStatus.IsActive())
                        .Include(rt => rt.ContentPublicationRequest)
                        .ToList();
                    selectionGroupList.Sections.Add(new NestedListSection
                    {
                        Name = group.GroupName,
                        Id = group.Id,
                        // Mark reduction tasks as cancelable
                        // The reduction task must either have no publication request or a nonactive publication request
                        Marked = reductionTasks
                            .Where(rt => rt.ContentPublicationRequest == null || !rt.ContentPublicationRequest.RequestStatus.IsActive())
                            .Any(),
                    });
                }
                foreach (var user in selectionGroupUsersDictionary[group.Id])
                {
                    selectionGroupList
                        .Sections.Single(s => s.Name == group.GroupName)
                        .Values.Add($"{user.FirstName} {user.LastName}");
                }
            }

            SelectionGroups = selectionGroupList;
        }
    }
}
