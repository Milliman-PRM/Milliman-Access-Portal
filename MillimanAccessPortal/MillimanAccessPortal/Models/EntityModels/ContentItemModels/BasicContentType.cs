using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    /// <summary>
    /// A simplified representation of a ContentType.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicContentType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool CanReduce { get; set; }
        public List<string> FileExtensions { get; set; }
    }
}
