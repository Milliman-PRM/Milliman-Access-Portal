using MapDbContextLib.Context;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private ContentPublicationRequest _publicationWhereContentItem(Guid contentItemId)
        {
            var publicationRequest = _dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == contentItemId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();

            return publicationRequest;
        }

        /// <summary>
        /// Select the most recent reduction for a selection group.
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>Most recent reduction request for the selection group</returns>
        private ContentReductionTask _reductionWhereSelectionGroup(Guid selectionGroupId)
        {
            var reductionTask = _dbContext.ContentReductionTask
                .Where(t => t.SelectionGroupId == selectionGroupId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();

            return reductionTask;
        }
        #endregion

        /// <summary>
        /// Select the most recent publication for each content item in a list.
        /// </summary>
        /// <param name="contentItemIds">List of content item IDs</param>
        /// <returns>List of most recent publications</returns>
        internal List<BasicPublication> SelectPublicationsWhereContentItemIn(List<Guid> contentItemIds)
        {
            var publications = new List<BasicPublication> { };
            foreach (var contentItemId in contentItemIds)
            {
                var publicationRequest = _publicationWhereContentItem(contentItemId);

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
        internal List<PublicationQueueDetails> SelectQueueDetailsWherePublicationIn(List<Guid> publicationIds)
        {
            var queueDetails = new List<PublicationQueueDetails> { };
            foreach (var publicationId in publicationIds)
            {
                var publication = _dbContext.ContentPublicationRequest
                    .Single(r => r.Id == publicationId);

                // Provide queue position for publications that have not yet begun
                if (publication.RequestStatus.IsCancelable())
                {
                    var precedingPublicationRequestCount = _dbContext.ContentPublicationRequest
                        .Where(r => r.CreateDateTimeUtc < publication.CreateDateTimeUtc)
                        .Where(r => r.RequestStatus.IsCancelable())
                        .Count();
                    queueDetails.Add(new PublicationQueueDetails
                    {
                        PublicationId = publicationId,
                        QueuePosition = precedingPublicationRequestCount + 1,
                    });
                }
                // Provide progress details for publications that have begun
                else if (publication.RequestStatus.IsActive())
                {
                    var reductionTasks = _dbContext.ContentReductionTask
                        .Where(t => t.ContentPublicationRequestId == publicationId)
                        .Where(t => t.SelectionGroupId != null)  // exclude hierarchy extract task
                        .Select(t => t.ReductionStatus)
                        .ToList();
                    var reductionsCompleted = reductionTasks.Where(t => t == ReductionStatusEnum.Reduced).Count();
                    var reductionsTotal = reductionTasks.Count;
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
        internal List<BasicReduction> SelectReductionsWhereSelectionGroupIn(List<Guid> selectionGroupIds)
        {
            var reductions = new List<BasicReduction> { };
            foreach (var selectionGroupId in selectionGroupIds)
            {
                var reductionTask = _reductionWhereSelectionGroup(selectionGroupId);

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
        internal List<ReductionQueueDetails> SelectQueueDetailsWhereReductionIn(List<Guid> reductionIds)
        {
            var queueDetails = new List<ReductionQueueDetails> { };
            foreach (var reductionId in reductionIds)
            {
                var reduction = _dbContext.ContentReductionTask
                    .Single(r => r.Id == reductionId);

                // Provide queue position for reductions that have not yet begun
                if (reduction.ReductionStatus.IsCancelable())
                {
                    var precedingReductionTaskCount = _dbContext.ContentReductionTask
                        .Where(r => r.CreateDateTimeUtc < reduction.CreateDateTimeUtc)
                        .Where(r => r.ReductionStatus.IsCancelable())
                        .Count();
                    queueDetails.Add(new ReductionQueueDetails
                    {
                        ReductionId = reductionId,
                        QueuePosition = precedingReductionTaskCount + 1,
                    });
                }
            }

            return queueDetails;
        }

        /// <summary>
        /// Select the list of selections for a reduction task
        /// </summary>
        /// <param name="selectionGroupId">Selection group ID</param>
        /// <returns>List of value IDs</returns>
        internal List<Guid> SelectReductionSelections(Guid selectionGroupId)
        {
            var reductionTask = _reductionWhereSelectionGroup(selectionGroupId);
            if (reductionTask == null)
            {
                return new List<Guid> { };
            }

            var selections = _dbContext.ContentReductionTask
                .Where(t => t.Id == reductionTask.Id)
                .Select(t => t.SelectionCriteriaObj)
                .SingleOrDefault();

            return selections?.GetSelectedValueIds();
        }
    }
}
