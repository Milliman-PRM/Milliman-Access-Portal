using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common {
    /// <summary>
    /// This class is used to associate name/value pairs.  The name should match the name of one of the
    /// QVW data model fields( case sensitive ) with the value being the selected value.
    /// </summary>
    public class NVPair {
        /// <summary>
        /// Data model field name - case sensitive
        /// </summary>
        public string FieldName { get; set; }
        public bool IsNumeric { get; set; }
        /// <summary>
        /// The value of the field that should be selected
        /// </summary>
        public string Value { get; set; }

        public NVPair() {

        }
        public NVPair(string _Key, string _Value) {
            FieldName = _Key;
            Value = _Value;
        }
    }
}
