using System;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
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
