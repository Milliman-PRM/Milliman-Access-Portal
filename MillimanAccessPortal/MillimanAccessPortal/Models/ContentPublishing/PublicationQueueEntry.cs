/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublicationQueueEntry
    {
        public int QueuePosition { get; set; }
        public int ReductionsCompleted { get; set; }
        public int ReductionsTotal { get; set; }

        public static Dictionary<Guid,PublicationQueueEntry> Build(ApplicationDbContext dbContext, Client client)
        {
            Dictionary<Guid, PublicationQueueEntry> returnDict = new Dictionary<Guid, PublicationQueueEntry>();

            var requests = dbContext.ContentPublicationRequest.Where(r => PublicationStatusExtensions.ActiveStatuses.Contains(r.RequestStatus))
                                                              .OrderByDescending(r => r.CreateDateTimeUtc)
                                                              .ToList();

            requests.Aggregate(seed: 0, func: (int i, ContentPublicationRequest r) => 
                {
                    returnDict.Add(r.Id, new PublicationQueueEntry
                        {
                            QueuePosition = i,
                            ReductionsTotal = dbContext.ContentReductionTask.Count(t => t.ContentPublicationRequestId == r.Id),
                            ReductionsCompleted = dbContext.ContentReductionTask.Count(t => t.ContentPublicationRequestId == r.Id && 
                                                                                            t.ReductionStatus == ReductionStatusEnum.Reduced),
                        });
                    return ++i;
                });

            return returnDict;
        }
    }
}
