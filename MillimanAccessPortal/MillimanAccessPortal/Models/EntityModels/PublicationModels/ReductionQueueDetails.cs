using System;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class ReductionQueueDetails
    {
        public Guid ReductionId { get; set; }
        public int QueuePosition { get; set; }
        public int QueueTotal { get; set; }
    }
}
