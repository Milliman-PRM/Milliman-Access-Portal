using System;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class PublicationQueueDetails
    {
        public Guid PublicationId { get; set; }
        public int QueuePosition { get; set; }
        public int ReductionsCompleted { get; set; }
        public int ReductionsTotal { get; set; }
    }
}
