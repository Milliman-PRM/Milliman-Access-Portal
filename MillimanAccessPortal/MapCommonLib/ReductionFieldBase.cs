using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib;
using MapDbContextLib.Context;

namespace MapCommonLib
{
    public class ReductionFieldBase
    {
        public ReductionFieldBase() { }

        public ReductionFieldBase(HierarchyField Field, IEnumerable<HierarchyFieldValue> Vals)
        {
            this.FieldName = Field.FieldName;
            this.FieldDisplayName = Field.FieldDisplayName;
            this.StructureType = Field.StructureType;
            this.FieldDelimiter = Field.FieldDelimiter;
            this.FieldValues = Vals.Select(v => v.Value).ToArray();
        }

        public string FieldName { get; set; } = string.Empty;
        public string FieldDisplayName { get; set; } = string.Empty;
        public FieldStructureType StructureType { get; set; } = FieldStructureType.Unknown;
        public string FieldDelimiter { get; set; } = null;
        public string[] FieldValues { get; set; } = new string[0];
    }
}

