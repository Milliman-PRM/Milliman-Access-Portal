using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class DeleteGroupResponseModel
    {
        /// <summary>
        /// ID of the group that was deleted.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Card stats for the selected content item.
        /// </summary>
        public BasicContentItemWithCardStats ContentItemStats { get; set; }
    }
}
