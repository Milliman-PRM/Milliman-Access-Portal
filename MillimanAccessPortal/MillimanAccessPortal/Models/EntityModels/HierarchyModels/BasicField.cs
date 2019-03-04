using System;

namespace MillimanAccessPortal.Models.EntityModels.HierarchyModels
{
    /// <summary>
    /// A simplified representation of a HierarchyField.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicField
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
    }
}
