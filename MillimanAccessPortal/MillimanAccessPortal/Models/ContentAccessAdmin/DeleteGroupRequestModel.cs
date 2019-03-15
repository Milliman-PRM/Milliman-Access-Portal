using MapCommonLib.ActionFilters;
using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class DeleteGroupRequestModel
    {
        /// <summary>
        /// ID of the selection group to delete.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid GroupId { get; set; }
    }
}
