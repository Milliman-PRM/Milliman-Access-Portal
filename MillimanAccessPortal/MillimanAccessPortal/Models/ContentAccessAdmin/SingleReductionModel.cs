using MillimanAccessPortal.Models.EntityModels.PublicationModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SingleReductionModel
    {
        public BasicSelectionGroup Group { get; set; }
        public BasicReduction Reduction { get; set; }
        public ReductionQueueDetails ReductionQueue { get; set; }
    }
}
