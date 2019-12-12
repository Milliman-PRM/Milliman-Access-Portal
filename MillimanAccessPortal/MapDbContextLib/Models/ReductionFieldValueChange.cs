/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model component class to be used in building the overall publication preview model
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public enum FieldValueChange
    {
        NoChange = 0,
        Added = 1,
        Removed = 2,
    }

    public class ReductionFieldValueChange : ReductionFieldValue
    {
        public FieldValueChange ValueChange = FieldValueChange.NoChange;
    }
}
