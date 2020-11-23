using MillimanAccessPortal.Models.EntityModels.ClientModels;
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

        /// <summary>
        /// All reductions for the associated selection groups.
        /// </summary>
        public Dictionary<Guid, BasicReduction> Reductions { get; set; }
        public Dictionary<Guid, ReductionQueueDetails> ReductionQueue { get; set; }

        /// <summary>
        /// Card stats for the selected content item.
        /// </summary>
        public BasicContentItemWithCardStats ContentItemStats { get; set; }

        /// <summary>
        /// Card stats for the selected client.
        /// </summary>
        public BasicClientWithCardStats ClientStats { get; set; }
    }
}
