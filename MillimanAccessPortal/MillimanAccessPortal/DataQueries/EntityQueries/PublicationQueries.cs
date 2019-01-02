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
    }
}
