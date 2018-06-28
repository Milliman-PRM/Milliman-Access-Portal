/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.DataQueries;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionsDetail
    {
        public ContentReductionHierarchy<ReductionFieldValueSelection> Hierarchy { get; set; }
        public long[] OriginalSelections { get; set; } = { };
        public ReductionSummary ReductionDetails { get; set; }

        internal static SelectionsDetail Build(ApplicationDbContext DbContext, StandardQueries Queries, SelectionGroup SelectionGroup)
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
                        if (value.SelectionStatus)
                        {
                            SelectedValues.Add(value.Id);
                        }
                    }
                }
                SelectedValuesArray = SelectedValues.ToArray();
            }

            var latestTask = DbContext.ContentReductionTask
                .Include(crt => crt.ContentPublicationRequest)
                .Where(crt => crt.SelectionGroupId == SelectionGroup.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();
            ReductionSummary reductionDetails = ((ReductionSummary) latestTask);

            SelectionsDetail model = new SelectionsDetail
            {
                Hierarchy = ContentReductionHierarchy< ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(DbContext, SelectionGroup.Id, SelectedValuesArray),
                OriginalSelections = DbContext.SelectionGroup
                    .Where(sg => sg.Id == SelectionGroup.Id)
                    .Select(sg => sg.SelectedHierarchyFieldValueList)
                    .Single(),
                ReductionDetails = reductionDetails,
            };

            return model;
        }
    }
}
