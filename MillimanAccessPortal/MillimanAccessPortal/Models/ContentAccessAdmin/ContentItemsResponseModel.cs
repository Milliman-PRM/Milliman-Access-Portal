using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ContentItemsResponseModel
    {
        /// <summary>
        /// Content items for the currently selected client.
        /// </summary>
        public Dictionary<Guid, BasicContentItemWithCardStats> ContentItems { get; set; }

        /// <summary>
        /// Content types assigned to content items in this model.
        /// </summary>
        public Dictionary<Guid, BasicContentType> ContentTypes { get; set; }

        /// <summary>
        /// Most recent publications for content items in this model.
        /// </summary>
        public Dictionary<Guid, BasicPublication> Publications { get; set; }
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; }

        /// <summary>
        /// Card stats for the currently selected client.
        /// </summary>
        public BasicClientWithCardStats ClientStats { get; set; }
    }
}
