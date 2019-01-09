using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class StatusResponseModel
    {
        public Dictionary<Guid, BasicPublication> Publications { get; set; }
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; }
        public Dictionary<Guid, BasicReduction> Reductions { get; set; }
        public Dictionary<Guid, ReductionQueueDetails> ReductionQueue { get; set; }
    }
}
