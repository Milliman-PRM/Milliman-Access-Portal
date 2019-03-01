using MapCommonLib.ActionFilters;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SuspendGroupRequestModel
    {
        /// <summary>
        /// ID of the selection group to update.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid GroupId { get; set; }

        /// <summary>
        /// Whether the selection group will be set as suspended.
        /// </summary>
        [EmitBeforeAfterLog]
        public bool IsSuspended { get; set; }
    }
}
