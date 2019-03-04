using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    /// <summary>
    /// Information to update the front end after a reduction is queued or canceled.
    /// </summary>
    public class SingleReductionModel
    {
        public BasicSelectionGroup Group { get; set; }

        public BasicReduction Reduction { get; set; }

        public ReductionQueueDetails ReductionQueue { get; set; }

        /// <summary>
        /// Live selections for the selection group
        /// </summary>
        public List<Guid> LiveSelections { get; set; }
    }
}
