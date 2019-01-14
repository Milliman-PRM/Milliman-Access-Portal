using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SingleReductionModel
    {
        public BasicSelectionGroup Group { get; set; }
        public BasicReduction Reduction { get; set; }
        public ReductionQueueDetails ReductionQueue { get; set; }
        public List<Guid> LiveSelections { get; set; }
    }
}
