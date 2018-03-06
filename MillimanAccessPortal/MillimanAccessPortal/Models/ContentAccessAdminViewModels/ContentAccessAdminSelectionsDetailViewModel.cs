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
using Newtonsoft.Json;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionsDetailViewModel
    {
        // TODO: Include an attribute for pending selections
        // TODO: Include an attribute for the user responsible for current status
        public ContentReductionHierarchy<ReductionFieldValueSelection> Hierarchy { get; set; }
        public long[] OriginalSelections { get; set; } = { };
        public object Status { get; set; }

        internal static ContentAccessAdminSelectionsDetailViewModel Build(ApplicationDbContext DbContext, StandardQueries Queries, SelectionGroup SelectionGroup)
        {
            var OutstandingStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued,
                ReductionStatusEnum.Reducing,
                ReductionStatusEnum.Reduced,
            };
            var ContentReductionTask = DbContext.ContentReductionTask
                .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                .Where(crt => OutstandingStatus.Contains(crt.ReductionStatus))
                .SingleOrDefault();
            // Convert the serialized content reduction hierarchy into a list of selected values
            long[] SelectedValuesArray = null;
            if (ContentReductionTask != null)
            {
                var SelectedValues = new List<long>();
                var Hierarchy = ContentReductionHierarchy<ReductionFieldValueSelection>.DeserializeJson(ContentReductionTask.SelectionCriteria);
                foreach (var field in Hierarchy.Fields)
                {
                    foreach (var value in field.Values)
                    {
                        if (!value.HasSelectionStatus)
                        {
                            continue;
                        }
                        var valueWithSelection = ((ReductionFieldValueSelection)value);
                        if (valueWithSelection.SelectionStatus)
                        {
                            SelectedValues.Add(valueWithSelection.Id);
                        }
                    }
                }
                SelectedValuesArray = SelectedValues.ToArray();
            }

            ContentAccessAdminSelectionsDetailViewModel Model = new ContentAccessAdminSelectionsDetailViewModel
            {
                Hierarchy = Queries.GetFieldSelectionsForSelectionGroup(SelectionGroup.Id, SelectedValuesArray),
                OriginalSelections = DbContext.SelectionGroup
                    .Where(sg => sg.Id == SelectionGroup.Id)
                    .Select(sg => sg.SelectedHierarchyFieldValueList)
                    .Single(),
                Status = DbContext.ContentReductionTask
                    .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                    .OrderByDescending(crt => crt.CreateDateTime)
                    .Select(crt => new
                    {
                        StatusEnum = crt.ReductionStatus,
                        DisplayName = ContentReductionTask.ReductionStatusDisplayNames[crt.ReductionStatus],
                        Creator = crt.ApplicationUser,
                    })
                    .FirstOrDefault(),
            };

            return Model;
        }
    }
}
