using AuditLogLib.Services;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
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

        internal async Task<List<BasicPublication>> SelectPublicationsWhereContentItemIn(List<Guid> contentItemIds)
        {
            var publications = new List<BasicPublication> { };
            foreach (var contentItemId in contentItemIds)
            {
                var publicationRequest = await _dbContext.ContentPublicationRequest
                    .Where(r => r.RootContentItemId == contentItemId)
                    .OrderByDescending(r => r.CreateDateTimeUtc)
                    .FirstOrDefaultAsync();

                var publication = (BasicPublication)publicationRequest;
                if (publication != null)
                {
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
            foreach (var contentItemId in selectionGroupIds)
            {
                var reductionTask = await _dbContext.ContentReductionTask
                    .Where(t => t.SelectionGroupId == contentItemId)
                    .OrderByDescending(r => r.CreateDateTimeUtc)
                    .FirstOrDefaultAsync();

                var reduction = (BasicReduction)reductionTask;
                if (reduction != null)
                {
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
    }
}
