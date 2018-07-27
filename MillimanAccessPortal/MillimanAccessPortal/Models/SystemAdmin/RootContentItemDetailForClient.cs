using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentDetailForClient
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        public Dictionary<string, List<string>> SelectionGroups { get; set; }
    }
}
