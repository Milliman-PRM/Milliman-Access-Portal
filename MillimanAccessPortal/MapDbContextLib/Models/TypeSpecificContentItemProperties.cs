using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class TypeSpecificContentItemProperties
    {}

    public class PowerBiContentItemProperties : TypeSpecificContentItemProperties
    {
        public bool FilterPaneEnabled { get; set; }

        public bool NavigationPaneEnabled { get; set; }
    }
}
