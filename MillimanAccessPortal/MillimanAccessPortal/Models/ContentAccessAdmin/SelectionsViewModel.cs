using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using MillimanAccessPortal.Models.EntityModels.SelectionGroupModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionsViewModel
    {
        public SelectionGroupSelections Selections { get; set; }
        public Dictionary<Guid, BasicField> Fields { get; set; }
        public Dictionary<Guid, BasicValue> Values { get; set; }
    }
}
