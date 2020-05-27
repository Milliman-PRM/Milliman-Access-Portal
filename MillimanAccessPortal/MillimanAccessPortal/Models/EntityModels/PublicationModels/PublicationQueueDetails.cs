/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    public class PublicationQueueDetails
    {
        /// <summary>
        /// The ContentPublicationRequest these details are associated with
        /// </summary>
        public Guid PublicationId { get; set; }

        /// <summary>
        /// The number queue position of this publication
        /// </summary>
        public int QueuePosition { get; set; }

        /// <summary>
        /// The number of reductions completed for this publication
        /// </summary>
        public int ReductionsCompleted { get; set; }

        /// <summary>
        /// The total number of reductions for this publication
        /// </summary>
        public int ReductionsTotal { get; set; }
    }
}
