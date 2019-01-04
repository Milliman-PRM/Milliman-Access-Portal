using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private async Task<ContentPublicationRequest> _publicationWhereContentItem(Guid contentItemId)
        {
            var publicationRequest = await _dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == contentItemId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefaultAsync();

            return publicationRequest;
        }

        private async Task<ContentReductionTask> _reductionWhereSelectionGroup(Guid selectionGroupId)
        {
            var reductionTask = await _dbContext.ContentReductionTask
                .Where(t => t.SelectionGroupId == selectionGroupId)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefaultAsync();

            return reductionTask;
        }

        internal async Task<List<BasicPublication>> SelectPublicationsWhereContentItemIn(List<Guid> contentItemIds)
        {
            var publications = new List<BasicPublication> { };
            foreach (var contentItemId in contentItemIds)
            {
                var publicationRequest = await _publicationWhereContentItem(contentItemId);

                if (publicationRequest != null)
                {
                    var publication = (BasicPublication)publicationRequest;
                    publications.Add(publication);
                }
            }

            return publications;
        }

        internal async Task<List<PublicationQueueDetails>> SelectQueueDetailsWherePublicationIn(
            List<Guid> publicationIds)
        {
            var queueDetails = new List<PublicationQueueDetails> { };
            foreach (var publicationId in publicationIds)
            {
                var publication = await _dbContext.ContentPublicationRequest
                    .SingleAsync(r => r.Id == publicationId);
                if (publication.RequestStatus.IsCancelable())
                {
                    var precedingPublicationRequestCount = await _dbContext.ContentPublicationRequest
                        .Where(r => r.CreateDateTimeUtc < publication.CreateDateTimeUtc)
                        .Where(r => r.RequestStatus.IsCancelable())
                        .CountAsync();
                    queueDetails.Add(new PublicationQueueDetails
                    {
                        PublicationId = publicationId,
                        QueuePosition = precedingPublicationRequestCount,
                    });
                }
            }

            return queueDetails;
        }

        internal async Task<List<BasicReduction>> SelectReductionsWhereSelectionGroupIn(List<Guid> selectionGroupIds)
        {
            var reductions = new List<BasicReduction> { };
            foreach (var selectionGroupId in selectionGroupIds)
            {
                var reductionTask = await _reductionWhereSelectionGroup(selectionGroupId);

                if (reductionTask != null)
                {
                    var reduction = (BasicReduction)reductionTask;
                    reductions.Add(reduction);
                }
            }

            return reductions;
        }

        internal async Task<List<ReductionQueueDetails>> SelectQueueDetailsWhereReductionIn(
            List<Guid> reductionIds)
        {
            var queueDetails = new List<ReductionQueueDetails> { };
            foreach (var reductionId in reductionIds)
            {
                var reduction = await _dbContext.ContentReductionTask
                    .SingleAsync(r => r.Id == reductionId);
                if (reduction.ReductionStatus.IsCancelable())
                {
                    var precedingReductionTaskCount = _dbContext.ContentReductionTask
                        .Where(r => r.CreateDateTimeUtc < reduction.CreateDateTimeUtc)
                        .Where(r => r.ReductionStatus.IsCancelable())
                        .Count();
                    queueDetails.Add(new ReductionQueueDetails
                    {
                        ReductionId = reductionId,
                        QueuePosition = precedingReductionTaskCount,
                    });
                }
            }

            return queueDetails;
        }

        internal async Task<List<Guid>> SelectReductionSelections(Guid selectionGroupId)
        {
            var reductionTask = await _reductionWhereSelectionGroup(selectionGroupId);
            if (reductionTask == null)
            {
                return new List<Guid> { };
            }

            var selections = await _dbContext.ContentReductionTask
                .Where(t => t.Id == reductionTask.Id)
                .Select(t => t.SelectionCriteriaObj)
                .SingleOrDefaultAsync();

            return selections?.GetSelectedValueIds();
        }
    }
}
