using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    /// <summary>
    /// Provides queries related to publications and reductions.
    /// </summary>
    public class PublicationQueries
    {
        private readonly ApplicationDbContext _dbContext;

        public PublicationQueries(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region private queries
        /// <summary>
        /// Select the most recent publication for a content item.
        /// </summary>
        /// <param name="contentItemId">Content item ID</param>
        /// <returns>Most recent publication request for the content item</returns>
        private async Task<ContentPublicationRequest> PublicationWhereContentItemAsync(Guid contentItemId)
        {
            DateTime onlyRequestsOnOrAfter = (await _dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == contentItemId)
                .Where(r => r.RequestStatus == PublicationStatus.Confirmed)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefaultAsync())
                ?.CreateDateTimeUtc
                ?? DateTime.MinValue;

            var publicationRequest = await _dbContext.ContentPublicationRequest
                .Include(r => r.ApplicationUser)
                .Where(r => r.RootContentItemId == contentItemId)
                .Where(r => PublicationStatusExtensions.CurrentStatuses.Contains(r.RequestStatus))
                .Where(r => r.CreateDateTimeUtc >= onlyRequestsOnOrAfter)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefaultAsync();

            return publicationRequest;
        }

        /// <summary>
        /// Select the most recent reduction for a selection group.
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>Most recent reduction request for the selection group</returns>
        private async Task<ContentReductionTask> ReductionWhereSelectionGroupAsync(Guid selectionGroupId)
        {
            var reductionTask = await _dbContext.ContentReductionTask
                .Include(t => t.ApplicationUser)
                .Where(t => t.SelectionGroupId == selectionGroupId)
                .Where(t => ReductionStatusExtensions.accessAdminStatusList.Contains(t.ReductionStatus))
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefaultAsync();

            return reductionTask;
        }
        #endregion

        /// <summary>
        /// Select the most recent publication for each content item in a list.
        /// </summary>
        /// <param name="contentItemIds">List of content item IDs</param>
        /// <returns>List of most recent publications</returns>
        internal async Task<List<BasicPublication>> SelectPublicationsWhereContentItemInAsync(List<Guid> contentItemIds)
        {
            var publications = new List<BasicPublication> { };
            foreach (var contentItemId in contentItemIds)
            {
                var publicationRequest = await PublicationWhereContentItemAsync(contentItemId);

                if (publicationRequest != null)
                {
                    var publication = (BasicPublication)publicationRequest;
                    publications.Add(publication);
                }
            }

            return publications;
        }

        /// <summary>
        /// Build queue detail objects for each publication in a list.
        /// </summary>
        /// <param name="publicationIds">List of publication IDs</param>
        /// <returns>List of queue detail objects</returns>
        internal async Task<List<PublicationQueueDetails>> SelectQueueDetailsWherePublicationInAsync(List<Guid> publicationIds)
        {
            var queueDetails = new List<PublicationQueueDetails> { };
            foreach (var publicationId in publicationIds)
            {
                var publication = await _dbContext.ContentPublicationRequest
                    .SingleAsync(r => r.Id == publicationId);

                // Provide queue position for publications that have not yet begun
                if (PublicationStatusExtensions.QueueWaitableStatusList.Contains(publication.RequestStatus))
                {
                    var precedingPublicationRequestCount = await _dbContext.ContentPublicationRequest
                        .Where(r => r.CreateDateTimeUtc < publication.CreateDateTimeUtc)
                        .Where(r => PublicationStatusExtensions.QueueWaitableStatusList.Contains(r.RequestStatus))
                        .CountAsync();
                    queueDetails.Add(new PublicationQueueDetails
                    {
                        PublicationId = publicationId,
                        QueuePosition = precedingPublicationRequestCount,
                    });
                }
                // Provide progress details for publications that have begun
                else if (publication.RequestStatus.IsActive())
                {
                    var reductionTaskStatusList = await _dbContext.ContentReductionTask
                        .Where(t => t.ContentPublicationRequestId == publicationId)
                        .Where(t => t.SelectionGroupId != null)  // exclude hierarchy extract task
                        .Select(t => t.ReductionStatus)
                        .ToListAsync();
                    var reductionsCompleted = reductionTaskStatusList.Count(t => !ReductionStatusExtensions.cancelableStatusList.Contains(t));
                    var reductionsTotal = reductionTaskStatusList.Count;
                    queueDetails.Add(new PublicationQueueDetails
                    {
                        PublicationId = publicationId,
                        ReductionsCompleted = reductionsCompleted,
                        ReductionsTotal = reductionsTotal,
                    });
                }
            }

            return queueDetails;
        }

        /// <summary>
        /// Select the most recent reduction for each selection group in a list.
        /// </summary>
        /// <param name="selectionGroupIds">List of selection group IDs</param>
        /// <returns>List of most recent reductions</returns>
        internal async Task<List<BasicReduction>> SelectReductionsWhereSelectionGroupInAsync(List<Guid> selectionGroupIds)
        {
            var reductions = new List<BasicReduction> { };
            foreach (var selectionGroupId in selectionGroupIds)
            {
                var reductionTask = await ReductionWhereSelectionGroupAsync(selectionGroupId);

                if (reductionTask != null)
                {
                    var reduction = (BasicReduction)reductionTask;
                    reductions.Add(reduction);
                }
            }

            return reductions;
        }

        /// <summary>
        /// Build queue detail objects for each reduction in a list.
        /// </summary>
        /// <param name="reductionIds">List of reduction IDs</param>
        /// <returns>List of queue detail objects</returns>
        internal async Task<List<ReductionQueueDetails>> SelectQueueDetailsWhereReductionInAsync(List<Guid> reductionIds)
        {
            var queueDetails = new List<ReductionQueueDetails> { };
            foreach (var reductionId in reductionIds)
            {
                var reduction = await _dbContext.ContentReductionTask
                    .SingleAsync(r => r.Id == reductionId);

                // Provide queue position for reductions that have not yet begun
                if (reduction.ReductionStatus.IsCancelable())
                {
                    var precedingReductionTaskCount = await _dbContext.ContentReductionTask
                        .Where(r => r.CreateDateTimeUtc <= reduction.CreateDateTimeUtc)
                        .Where(r => ReductionStatusExtensions.cancelableStatusList.Contains(r.ReductionStatus))
                        .Where(r => r.Id != reduction.Id)
                        .CountAsync();
                    queueDetails.Add(new ReductionQueueDetails
                    {
                        ReductionId = reductionId,
                        QueuePosition = precedingReductionTaskCount,
                    });
                }
            }

            return queueDetails;
        }

        /// <summary>
        /// Select the list of selections for a reduction task
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>IDs of selected values minus any values not in the new reduced hierarchy</returns>
        internal async Task<List<Guid>> SelectReductionSelectionsAsync(Guid selectionGroupId)
        {
            var reductionTask = await ReductionWhereSelectionGroupAsync(selectionGroupId);
            if (reductionTask == null)
            {
                return new List<Guid> { };
            }

            ContentReductionHierarchy<ReductionFieldValueSelection> selections = (await _dbContext.ContentReductionTask.FindAsync(reductionTask.Id))
                ?.SelectionCriteriaObj;

            // remove selected values not contained in the hierarchy of the newly reduced content
            if (reductionTask.ReducedContentHierarchyObj != null && selections != null)
            {
                foreach (var selectionHierarchyField in selections.Fields)
                {
                    var reducedValueList = reductionTask.ReducedContentHierarchyObj
                        .Fields
                        .Single(f => f.FieldName == selectionHierarchyField.FieldName)
                        .Values
                        .Select(v => v.Value)
                        .ToList();

                    foreach (ReductionFieldValueSelection selectedValue in selectionHierarchyField.Values.Where(v => v.SelectionStatus))
                    {
                        if (!reducedValueList.Contains(selectedValue.Value))
                        {
                            selectedValue.SelectionStatus = false;
                        }
                    }
                }
            }
            else if (reductionTask.TaskAction != TaskActionEnum.HierarchyOnly && 
                     reductionTask.ReducedContentHierarchyObj == null && 
                     reductionTask.ReductionStatus == ReductionStatusEnum.Warning)
            {
                return new List<Guid>();
            }

            return selections?.GetSelectedValueIds();
        }
    }
}
