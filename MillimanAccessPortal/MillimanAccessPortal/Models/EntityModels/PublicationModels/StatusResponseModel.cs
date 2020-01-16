/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.ContentPublishing;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    /// <summary>
    /// Data returned for a publication status poll
    /// </summary>
    public class StatusResponseModel
    {
        /// <summary>
        /// List of publications for the selected client
        /// </summary>
        public Dictionary<Guid, BasicPublication> Publications { get; set; } = new Dictionary<Guid, BasicPublication>();
        public Dictionary<Guid, PublicationQueueDetails> PublicationQueue { get; set; } = new Dictionary<Guid, PublicationQueueDetails>();

        /// <summary>
        /// Content items for the selected client
        /// </summary>
        public Dictionary<Guid, RootContentItemNewSummary> ContentItems { get; set; } = new Dictionary<Guid, RootContentItemNewSummary>();
    }
}
