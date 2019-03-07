using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    /// <summary>
    /// Data returned for a publication/reduction status poll
    /// </summary>
    public class StatusResponseModel
    {
        /// <summary>
        /// List of publications for the selected client
        /// </summary>
        public Dictionary<Guid, BasicPublication> Publications { get; set; }
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; }
        
        /// <summary>
        /// List of reductions for the selected content item
        /// </summary>
        public Dictionary<Guid, BasicReduction> Reductions { get; set; }
        public Dictionary<Guid, ReductionQueueDetails> ReductionQueue { get; set; }

        /// <summary>
        /// Live selections for all selection groups in the selected content item
        /// </summary>
        public Dictionary<Guid, List<Guid>> LiveSelectionsSet { get; set; }

        /// <summary>
        /// Content items for the selected client
        /// </summary>
        public Dictionary<Guid, BasicContentItem> ContentItems { get; set; }

        /// <summary>
        /// Selection groups for the selected content item
        /// </summary>
        public Dictionary<Guid, BasicSelectionGroup> Groups { get; set; }
    }
}
