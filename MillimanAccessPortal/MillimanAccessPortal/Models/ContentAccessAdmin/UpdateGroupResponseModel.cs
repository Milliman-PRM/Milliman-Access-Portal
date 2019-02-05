using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateGroupResponseModel
    {
        public BasicSelectionGroupWithAssignedUsers Group { get; set; }
        public BasicContentItemWithStats ContentItemStats { get; set; }
    }
}
