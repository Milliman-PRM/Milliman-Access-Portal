using System;

namespace MillimanAccessPortal.Models.EntityModels.HierarchyModels
{
    public class BasicField
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
    }
}
