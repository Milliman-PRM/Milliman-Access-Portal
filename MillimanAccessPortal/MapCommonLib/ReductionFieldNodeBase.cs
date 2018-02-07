using System;
using System.Collections.Generic;
using System.Text;

namespace MapCommonLib
{
    public class ReductionFieldNodeBase
    {
        public string FieldName { get; set; } = string.Empty;
        public string[] FieldValues { get; set; } = new string[0];
        public ReductionFieldNodeBase[] ChildNodes { get; set; } = new ReductionFieldNodeBase[0];
    }
}

