using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class CreateGroupViewModel
    {
        public BasicSelectionGroupWithAssignedUsers Group { get; set; }
        public BasicContentItemWithStats ContentItemStats { get; set; }
        public BasicClientWithStats ClientStats { get; set; }
    }
}
