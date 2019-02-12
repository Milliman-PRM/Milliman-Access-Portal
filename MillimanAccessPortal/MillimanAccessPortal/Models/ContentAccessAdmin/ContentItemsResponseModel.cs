using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ContentItemsResponseModel
    {
        public Dictionary<Guid, BasicContentItemWithCardStats> ContentItems { get; set; }
        public Dictionary<Guid, BasicContentType> ContentTypes { get; set; }
        public Dictionary<Guid, BasicPublication> Publications { get; set; }
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; }

        public BasicClientWithCardStats ClientStats { get; set; }
    }
}
