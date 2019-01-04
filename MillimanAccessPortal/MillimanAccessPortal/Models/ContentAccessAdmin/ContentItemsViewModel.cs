using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ContentItemsViewModel
    {
        public Dictionary<Guid, BasicContentItemWithStats> Items { get; set; }
        public Dictionary<Guid, BasicContentType> ContentTypes { get; set; }
        public Dictionary<Guid, BasicPublication> Publications { get; set; }
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; }

        public BasicClientWithStats ClientStats { get; set; }
    }
}
