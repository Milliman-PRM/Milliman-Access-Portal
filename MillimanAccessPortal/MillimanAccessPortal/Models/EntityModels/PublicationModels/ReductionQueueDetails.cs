using System;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class ReductionQueueDetails
    {
        /// <summary>
        /// The ContentReductionTask these details are associated with
        /// </summary>
        public Guid ReductionId { get; set; }

        /// <summary>
        /// The number queue position of this reduction
        /// </summary>
        public int QueuePosition { get; set; }
    }
}
