using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    public class BasicSelectionGroupWithAssignedUsers : BasicSelectionGroup
    {
        /// <summary>
        /// A list of ApplicationUser ID's assigned to the selection group
        /// </summary>
        public List<Guid> AssignedUsers { get; set; }
    }
}
