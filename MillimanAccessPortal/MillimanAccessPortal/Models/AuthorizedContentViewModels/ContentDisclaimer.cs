using System;

namespace MillimanAccessPortal.Models.AuthorizedContentViewModels
{
    public class ContentDisclaimer
    {
        public Guid SelectionGroupId { get; set; }
        public string ContentName { get; set; }
        public string DisclaimerText { get; set; }
    }
}
