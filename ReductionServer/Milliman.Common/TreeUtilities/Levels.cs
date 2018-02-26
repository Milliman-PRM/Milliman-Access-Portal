using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common.TreeUtilities {
    /// <summary>
    /// Holds hierarchy level information while processing
    /// </summary>
    public class Levels {
        public string HierarchyName { get; set; }
        public string Concept { get; set; }
        public string DisplayLabel { get; set; }
        public List<string> DataModelName { get; set; }

        public Levels(string _HierarchyName, string _Concept, string _DisplayLabel) {
            HierarchyName = _HierarchyName;
            DisplayLabel = _DisplayLabel;
            Concept = _Concept;
            DataModelName = new List<string>();
        }
    }
}
