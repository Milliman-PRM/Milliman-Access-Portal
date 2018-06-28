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
        public string SelectionGroupName { get; set; }
        public string RootContentItemName { get; set; }
        public ReductionSummary ReductionSummary { get; set; }
        public SelectionComparison SelectionComparison { get; set; }
        public bool IsSuspended { get; set; }

        internal static SelectionsDetail Build(ApplicationDbContext dbContext, StandardQueries queries, SelectionGroup selectionGroup)
        {
            if (selectionGroup.RootContentItem == null)
            {
                selectionGroup.RootContentItem = dbContext.RootContentItem.Find(selectionGroup.RootContentItemId);
            }

            // Query for the most recent reduction task for this selection group.
            var latestTask = dbContext.ContentReductionTask
                .Include(crt => crt.ContentPublicationRequest)
                .Where(crt => crt.SelectionGroupId == selectionGroup.Id)
                .OrderByDescending(crt => crt.CreateDateTimeUtc)
                .FirstOrDefault();

            #region Build selection comparison

            // Define status that qualify the latest task as an outstanding task.
            // An outstanding task blocks other reductions for its selection group.
            var outstandingStatus = new List<ReductionStatusEnum>
            {
                ReductionStatusEnum.Queued,
                ReductionStatusEnum.Reducing,
                ReductionStatusEnum.Reduced,
            };

            // Build the live hierarchy and list of live selections
            var liveHierarchy = ContentReductionHierarchy<ReductionFieldValueSelection>
                .GetFieldSelectionsForSelectionGroup(dbContext, selectionGroup.Id);

            var liveSelectionSet = selectionGroup.SelectedHierarchyFieldValueList.ToHashSet();

            // Convert the serialized content reduction hierarchy into a list of selected values
            HashSet<long> pendingSelectionSet = null;
            if (outstandingStatus.Contains(latestTask.ReductionStatus))
            {
                pendingSelectionSet = new HashSet<long>();
                var hierarchy = ContentReductionHierarchy<ReductionFieldValueSelection>
                    .DeserializeJson(latestTask.SelectionCriteria);
                foreach (var field in hierarchy.Fields)
                {
                    foreach (var value in field.Values)
                    {
                        if (value.SelectionStatus)
                        {
                            pendingSelectionSet.Add(value.Id);
                        }
                    }
                }
            }

            #region Diff live and pending selections

            var liveSelections = new List<SelectionDetails>();
            var pendingSelections = pendingSelectionSet == null
                ? null
                : new List<SelectionDetails>();

            foreach (var field in liveHierarchy.Fields)
            {
                foreach (var value in field.Values)
                {
                    var inLive = liveSelectionSet.Contains(value.Id);
                    var inPending = pendingSelectionSet == null
                        ? false
                        : pendingSelectionSet.Contains(value.Id);

                    if (inLive)
                    {
                        liveSelections.Add(new SelectionDetails
                        {
                            Id = value.Id,
                            Marked = !inPending,
                        });
                    }
                    if (inPending)
                    {
                        pendingSelections.Add(new SelectionDetails
                        {
                            Id = value.Id,
                            Marked = !inLive,
                        });
                    }
                }
            }

            #endregion

            #endregion

            SelectionsDetail model = new SelectionsDetail
            {
                SelectionGroupName = selectionGroup.GroupName,
                RootContentItemName = selectionGroup.RootContentItem.ContentName,
                ReductionSummary = ((ReductionSummary) latestTask),
                SelectionComparison = new SelectionComparison
                {
                    Hierarchy = liveHierarchy,
                    LiveSelections = liveSelections,
                    PendingSelections = pendingSelections,
                    IsLiveMaster = selectionGroup.IsMaster,
                },
                IsSuspended = selectionGroup.IsSuspended,
            };

            return model;
        }
    }

    public class SelectionComparison
    {
        public ContentReductionHierarchy<ReductionFieldValueSelection> Hierarchy { get; set; }
        public List<SelectionDetails> LiveSelections { get; set; }
        public List<SelectionDetails> PendingSelections { get; set; } = null;
        public bool IsLiveMaster { get; set; }
        public bool IsPendingMaster { get => false; }
    }

    public class SelectionDetails
    {
        public long Id { get; set; }
        public bool Marked { get; set; }
    }
}
