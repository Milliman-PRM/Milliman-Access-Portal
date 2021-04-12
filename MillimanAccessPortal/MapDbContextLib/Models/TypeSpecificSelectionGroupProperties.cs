/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: A family of model classes representing selection group properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public class TypeSpecificSelectionGroupProperties
    { }

    public class PowerBiSelectionGroupProperties : TypeSpecificSelectionGroupProperties
    {
        public bool Editable { get; set; }
    }
}
