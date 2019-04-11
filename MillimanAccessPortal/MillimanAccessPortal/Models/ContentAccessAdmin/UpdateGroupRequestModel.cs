using MapCommonLib.ActionFilters;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateGroupRequestModel
    {
        /// <summary>
        /// ID of the selection group to update.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid GroupId { get; set; }

        /// <summary>
        /// New name for the selection group.
        /// </summary>
        [EmitBeforeAfterLog]
        public string Name { get; set; }

        /// <summary>
        /// List of all users to be assigned to the selection group after processing.
        /// </summary>
        [EmitBeforeAfterLog]
        public List<Guid> Users { get; set; }
    }
}
