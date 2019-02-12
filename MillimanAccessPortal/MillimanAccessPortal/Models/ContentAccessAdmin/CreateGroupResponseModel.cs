using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class CreateGroupResponseModel
    {
        public BasicSelectionGroupWithAssignedUsers Group { get; set; }
        public BasicContentItemWithCardStats ContentItemStats { get; set; }
    }
}
