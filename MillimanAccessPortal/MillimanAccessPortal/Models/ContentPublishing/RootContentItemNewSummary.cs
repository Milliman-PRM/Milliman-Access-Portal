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
using System.Threading.Tasks;

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

        internal static async Task<RootContentItemNewSummary> BuildAsync(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var model = new RootContentItemNewSummary
            {
                Id = rootContentItem.Id,
                Name = rootContentItem.ContentName,
                ContentTypeId = rootContentItem.ContentTypeId,
                SelectionGroupCount = await dbContext.SelectionGroup.CountAsync(sg => sg.RootContentItemId == rootContentItem.Id),
                AssignedUserCount = await dbContext.UserInSelectionGroup
                                             .Where(usg => usg.SelectionGroup.RootContentItemId == rootContentItem.Id)
                                             .Select(usg => usg.UserId)
                                             .Distinct()
                                             .CountAsync(),
                IsSuspended = rootContentItem.IsSuspended,
                ClientId = rootContentItem.ClientId,
                DoesReduce = rootContentItem.DoesReduce,
            };

            return model;
        }
    }
}
