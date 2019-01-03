using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.SelectionGroupModels
{
    public class SelectionGroupSelections
    {
        public Guid Id { get; set; }
        public List<Guid> SelectedValues { get; set; }
    }
}
