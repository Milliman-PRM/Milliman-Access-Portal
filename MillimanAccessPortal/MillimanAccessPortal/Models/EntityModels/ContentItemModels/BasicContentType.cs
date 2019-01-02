using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    public class BasicContentType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool CanReduce { get; set; }
        public List<string> FileExtensions { get; set; }
    }
}
