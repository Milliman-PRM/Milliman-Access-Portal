using System;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib;

namespace MapCommonLib
{
    public class ReductionFieldBase
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldDisplayName { get; set; } = string.Empty;
        public string[] FieldValues { get; set; } = new string[0];
        public FieldStructureType InferredStructureType { get; set; } = FieldStructureType.Unknown;
        string FieldDelimiter { get; set; } = null;
    }
}

