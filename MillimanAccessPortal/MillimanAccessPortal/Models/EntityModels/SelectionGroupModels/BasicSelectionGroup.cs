using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    public class BasicSelectionGroup
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public List<Guid> SelectedValues { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsMaster { get; set; }
        public string Name { get; set; }
    }
}
