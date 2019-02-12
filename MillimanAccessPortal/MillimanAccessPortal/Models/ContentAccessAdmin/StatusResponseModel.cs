using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
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
        public Dictionary<Guid, List<Guid>> LiveSelectionsSet { get; set; }
        public Dictionary<Guid, BasicContentItem> ContentItems { get; set; }
        public Dictionary<Guid, BasicSelectionGroup> Groups { get; set; }
    }
}
