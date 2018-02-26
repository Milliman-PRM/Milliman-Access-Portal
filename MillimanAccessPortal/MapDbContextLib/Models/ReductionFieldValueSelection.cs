using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ReductionFieldValueSelection : ReductionFieldValue
    {
        public override bool HasSelectionStatus { get { return true; } }

        public ReductionFieldValueSelection()
        {}

        public ReductionFieldValueSelection(ReductionFieldValue Arg, bool StatusArg = false)
        {
            Value = Arg.Value;
            SelectionStatus = StatusArg;
        }

        public bool SelectionStatus { get; set; } = false;

    }
}
