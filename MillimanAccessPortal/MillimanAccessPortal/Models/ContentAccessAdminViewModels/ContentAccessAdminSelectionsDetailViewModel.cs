/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing details of a RootContentItem for use in ContentAccessAdmin
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;
using MapDbContextLib.Models;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionsDetailViewModel
    {
        // TODO: Include an attribute for pending selections
        // TODO: Include an attribute for the user responsible for current status
        public ContentReductionHierarchy Hierarchy { get; set; }
        public string Status { get; set; }

        internal static ContentAccessAdminSelectionsDetailViewModel Build(ApplicationDbContext DbContext, StandardQueries Queries, SelectionGroup SelectionGroup)
        {
            ContentAccessAdminSelectionsDetailViewModel Model = new ContentAccessAdminSelectionsDetailViewModel
            {
                Hierarchy = Queries.GetFieldSelectionsForSelectionGroup(SelectionGroup.Id),
                Status = DbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                    .OrderBy(crt => crt.CreateDateTime)
                    .Select(crt => ContentReductionTask.ReductionStatusDisplayNames[crt.ReductionStatus])
                    .LastOrDefault(),
            };

            return Model;
        }
    }
}
