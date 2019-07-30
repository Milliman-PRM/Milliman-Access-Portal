/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemsModel
    {
        public object ClientStats { get; set; }

        public Dictionary<Guid, RootContentItemNewSummary> ContentItems { get; set; } = new Dictionary<Guid, RootContentItemNewSummary>();

        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; } = new Dictionary<Guid, PublicationQueueDetails>();

        public Dictionary<Guid, BasicPublication> Publications { get; set; } = new Dictionary<Guid, BasicPublication>();
    }
}
