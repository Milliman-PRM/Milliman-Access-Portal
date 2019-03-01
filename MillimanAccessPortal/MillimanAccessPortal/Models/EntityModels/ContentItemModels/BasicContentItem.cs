using System;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    /// <summary>
    /// A simplified representation of a RootContentItem.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicContentItem
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Guid ContentTypeId { get; set; }
        public bool IsSuspended { get; set; }
        public bool DoesReduce { get; set; }
        public string Name { get; set; }
    }
}
