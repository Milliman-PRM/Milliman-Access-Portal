using System;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class DeleteGroupRequestModel
    {
        /// <summary>
        /// ID of the selection group to delete.
        /// </summary>
        public Guid GroupId { get; set; }
    }
}
