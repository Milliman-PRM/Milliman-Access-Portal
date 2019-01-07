using MillimanAccessPortal.Models.EntityModels.PublicationModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SingleReductionModel
    {
        public BasicReduction Reduction { get; set; }
        public ReductionQueueDetails ReductionQueue { get; set; }
    }
}
