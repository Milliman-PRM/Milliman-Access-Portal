using System;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class GoLiveViewModel
    {
        public Guid RootContentItemId { get; set; }
        public Guid PublicationRequestId { get; set; }
        public string ValidationSummaryId { get; set; }
    }
}
