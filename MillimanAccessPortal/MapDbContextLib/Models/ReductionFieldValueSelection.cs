/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents a value of a field of a selection hierarchy and whether the value is selected
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public class ReductionFieldValueSelection : ReductionFieldValue
    {
        /// <summary>
        /// Indicates whether this instance has a `SelectionStatus` property; follows the naming pattern of nullable<T>
        /// </summary>
        public override bool HasSelectionStatus { get => true; }

        /// <summary>
        /// Indicates whether this Value instance is selected or not selected
        /// </summary>
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
