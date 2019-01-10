using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class UpdateSelectionsRequestModel
    {
        /// <summary>
        /// ID of the selection group to update.
        /// </summary>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Whether the selection group will be set as a master group.
        /// </summary>
        public bool IsMaster { get; set; }

        /// <summary>
        /// List of selections to assign to the selection group.
        /// </summary>
        public List<Guid> Selections { get; set; }
    }
}
