/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.AccountViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemNewSummary
    {
        public int AssignedUserCount { get; set; }
        public Guid ClientId { get; set; }
        public Guid ContentTypeId { get; set; }
        public bool DoesReduce { get; set; }
        public Guid Id { get; set; }
        public bool IsSuspended { get; set; }
        public string Name { get; set; }
        public int SelectionGroupCount { get; set; }

        internal static RootContentItemNewSummary Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var model = new RootContentItemNewSummary
            {
                Id = rootContentItem.Id,
                Name = rootContentItem.ContentName,
                ContentTypeId = rootContentItem.ContentTypeId,
                SelectionGroupCount = dbContext.SelectionGroup.Count(sg => sg.RootContentItemId == rootContentItem.Id),
                AssignedUserCount = dbContext.UserInSelectionGroup
                                             .Where(usg => usg.SelectionGroup.RootContentItemId == rootContentItem.Id)
                                             .Select(usg => usg.UserId)
                                             .Distinct()
                                             .Count(),
                IsSuspended = rootContentItem.IsSuspended,
                ClientId = rootContentItem.ClientId,
                DoesReduce = rootContentItem.DoesReduce,
            };

            return model;
        }
    }
}
