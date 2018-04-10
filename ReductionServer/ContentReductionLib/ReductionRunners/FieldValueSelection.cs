/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents the selection status of a specific field value
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace ContentReductionLib.ReductionRunners
{
    internal class FieldValueSelection
    {
        internal string FieldName;
        internal string FieldValue;
        internal bool Selected;
    }
}
