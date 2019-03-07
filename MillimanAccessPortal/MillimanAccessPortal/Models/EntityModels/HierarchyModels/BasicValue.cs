using System;

namespace MillimanAccessPortal.Models.EntityModels.HierarchyModels
{
    /// <summary>
    /// A simplified representation of a HierarchyFieldValue.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicValue
    {
        public Guid Id { get; set; }
        public Guid ReductionFieldId { get; set; }
        public string Value { get; set; }
    }
}
