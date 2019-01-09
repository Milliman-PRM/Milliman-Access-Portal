using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionGroupsResponseModel
    {
        public Dictionary<Guid, BasicSelectionGroupWithAssignedUsers> Groups { get; set; }
        public Dictionary<Guid, BasicReduction> Reductions { get; set; }
        public Dictionary<Guid, ReductionQueueDetails> ReductionQueue { get; set; }

        public BasicContentItemWithStats ContentItemStats { get; set; }
        public BasicClientWithStats ClientStats { get; set; }
    }
}
