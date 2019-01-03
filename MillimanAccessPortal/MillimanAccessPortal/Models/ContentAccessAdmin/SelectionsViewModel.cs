using MillimanAccessPortal.Models.EntityModels.HierarchyModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class SelectionsViewModel
    {
        public Guid Id { get; set; }
        public List<Guid> LiveSelections { get; set; }
        public List<Guid> ReductionSelections { get; set; }
        public Dictionary<Guid, BasicField> Fields { get; set; }
        public Dictionary<Guid, BasicValue> Values { get; set; }
    }
}
