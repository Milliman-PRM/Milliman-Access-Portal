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

        public ReductionFieldValueSelection(ReductionFieldValue Arg)
        {
            Value = Arg.Value;
            SelectionStatus = false;
        }

        public bool SelectionStatus { get; set; } = false;

    }
}
