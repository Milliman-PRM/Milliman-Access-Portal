/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionGroupList
    {
        public List<SelectionGroupSummary> SelectionGroups = new List<SelectionGroupSummary>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static SelectionGroupList Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var model = new SelectionGroupList();

            var selectionGroups = dbContext.SelectionGroup
                .Where(sg => sg.RootContentItemId == rootContentItem.Id)
                .ToList();

            foreach (var selectionGroup in selectionGroups)
            {
                model.SelectionGroups.Add(
                    SelectionGroupSummary.Build(dbContext, selectionGroup)
                    );
            }

            return model;
        }
    }
}
