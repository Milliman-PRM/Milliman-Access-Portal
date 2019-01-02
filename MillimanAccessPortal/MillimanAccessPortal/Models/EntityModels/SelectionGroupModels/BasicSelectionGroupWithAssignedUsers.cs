using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    public class BasicSelectionGroupWithAssignedUsers : BasicSelectionGroup
    {
        public List<Guid> AssignedUsers { get; set; }
    }
}
