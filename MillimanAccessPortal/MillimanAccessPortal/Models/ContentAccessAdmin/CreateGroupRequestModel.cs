using MapCommonLib.ActionFilters;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class CreateGroupRequestModel
    {
        /// <summary>
        /// ID of the content item under which the new selection group will be created.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid ContentItemId { get; set; }

        /// <summary>
        /// Name of the new selection group.
        /// </summary>
        [EmitBeforeAfterLog]
        public string Name { get; set; }
    }
}
