using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class NestedList
    {
        public List<NestedListSection> Sections { get; set; } = new List<NestedListSection>();
    }

    public class NestedListSection
    {
        public string Name { get; set; }
        public List<string> Values { get; set; } = new List<string>();
    }
}
