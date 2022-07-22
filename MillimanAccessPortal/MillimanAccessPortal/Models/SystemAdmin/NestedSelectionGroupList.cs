/*
 * CODE OWNERS: Tejas George,
 * OBJECTIVE: Provide a basic data structure that defines a tree-like nested list.
 * DEVELOPER NOTES:
 *      This class is useful as it provides a simple interface to the front end for rendering
 *      nested lists of selection groups.
 */

using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class NestedSelectionGroupList
    {
        public List<NestedSelectionGroupListSection> Sections { get; set; } = new List<NestedSelectionGroupListSection>();
    }

    public class NestedSelectionGroupListSection
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public bool Suspended { get; set; }
        public List<string> Values { get; set; } = new List<string>();
    }
}
