using System;

namespace MillimanAccessPortal.Models.AuthorizedContentViewModels
{
    public class ContentDisclaimerModel
    {
        public string ValidationId { get; set; }
        public Guid SelectionGroupId { get; set; }
        public string ContentName { get; set; }
        public string DisclaimerText { get; set; }
    }
}
