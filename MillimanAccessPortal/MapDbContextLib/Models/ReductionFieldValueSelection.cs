/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents a hierarchy field value with a boolean field to indicate whether the value is selected
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public class ReductionFieldValueSelection : ReductionFieldValue
    {
        public override bool HasSelectionStatus { get => true; }
        public bool SelectionStatus { get; set; } = false;

        public ReductionFieldValueSelection()
        {}

        public ReductionFieldValueSelection(ReductionFieldValue Arg, bool StatusArg = false)
        {
            Value = Arg.Value;
            SelectionStatus = StatusArg;
        }


    }
}
