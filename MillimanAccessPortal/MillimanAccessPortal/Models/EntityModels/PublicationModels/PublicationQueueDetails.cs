/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class PublicationQueueDetails
    {
        /// <summary>
        /// The ContentPublicationRequest these details are associated with
        /// </summary>
        public Guid PublicationId { get; set; }

        /// <summary>
        /// The number queue position of this publication
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// The number of reductions completed for this publication
        /// </summary>
        public int ReductionsCompleted { get; set; }

        /// <summary>
        /// The total number of reductions for this publication
        /// </summary>
        public int ReductionsTotal { get; set; }

        public static Dictionary<Guid, PublicationQueueDetails> BuildQueueForClient(ApplicationDbContext dbContext, Client client)
        {
            Dictionary<Guid, PublicationQueueDetails> returnDict = new Dictionary<Guid, PublicationQueueDetails>();

            var requests = dbContext.ContentPublicationRequest.Where(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus))
                                                              .OrderByDescending(r => r.CreateDateTimeUtc)
                                                              .ToList();

            requests.Aggregate(seed: 0, func: (int i, ContentPublicationRequest r) =>
            {
                returnDict.Add(r.Id, new PublicationQueueDetails
                {
                    PublicationId = r.Id,
                    QueuePosition = i,
                    ReductionsTotal = dbContext.ContentReductionTask.Count(t => t.ContentPublicationRequestId == r.Id 
                                                                             && t.SelectionGroupId != null),
                    ReductionsCompleted = dbContext.ContentReductionTask.Count(t => t.ContentPublicationRequestId == r.Id
                                                                                 && t.SelectionGroupId != null
                                                                                 && (t.ReductionStatus == ReductionStatusEnum.Reduced || t.ReductionStatus == ReductionStatusEnum.Warning)),
                });
                return ++i;
            });

            return returnDict;
        }
    }
}
