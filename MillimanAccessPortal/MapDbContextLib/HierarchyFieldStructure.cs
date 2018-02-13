/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An enumerated set of interpretations that can be applied to a content reduction field
 * DEVELOPER NOTES: Defined in lowest of project dependencies since it is used in multiple projects
 */

namespace MapDbContextLib
{
    public enum FieldStructureType
    {
        Unknown,
        Flat,
        Tree
    }
}

