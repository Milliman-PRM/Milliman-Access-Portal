using System;

namespace MillimanAccessPortal.Models.EntityModels.HierarchyModels
{
    public class BasicValue
    {
        public Guid Id { get; set; }
        public Guid ReductionFieldId { get; set; }
        public string Value { get; set; }
    }
}
