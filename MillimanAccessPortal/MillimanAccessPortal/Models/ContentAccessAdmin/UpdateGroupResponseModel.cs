using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateGroupResponseModel
    {
        /// <summary>
        /// The selection group that was updated
        /// </summary>
        public BasicSelectionGroupWithAssignedUsers Group { get; set; }

        /// <summary>
        /// Card stats for the selected content item
        /// </summary>
        public BasicContentItemWithCardStats ContentItemStats { get; set; }
    }
}
