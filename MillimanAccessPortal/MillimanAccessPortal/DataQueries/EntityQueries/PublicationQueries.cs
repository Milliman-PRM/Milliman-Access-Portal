using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.DataQueries.EntityQueries
{
    public class PublicationQueries
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public PublicationQueries(
            IAuditLogger auditLogger,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogger = auditLogger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        private ContentPublicationRequest _publicationWhereContentItem(Guid contentItemId)
        {
            var publicationRequest = _dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == contentItemId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();

            return publicationRequest;
        }

        private ContentReductionTask _reductionWhereSelectionGroup(Guid selectionGroupId)
        {
            var reductionTask = _dbContext.ContentReductionTask
                .Where(t => t.SelectionGroupId == selectionGroupId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();

            return reductionTask;
        }

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

        internal List<PublicationQueueDetails> SelectQueueDetailsWherePublicationIn(
            List<Guid> publicationIds)
        {
            var queueDetails = new List<PublicationQueueDetails> { };
            foreach (var publicationId in publicationIds)
            {
                var publication = _dbContext.ContentPublicationRequest
                    .Single(r => r.Id == publicationId);
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

        internal List<ReductionQueueDetails> SelectQueueDetailsWhereReductionIn(
            List<Guid> reductionIds)
        {
            var queueDetails = new List<ReductionQueueDetails> { };
            foreach (var reductionId in reductionIds)
            {
                var reduction = _dbContext.ContentReductionTask
                    .Single(r => r.Id == reductionId);
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
