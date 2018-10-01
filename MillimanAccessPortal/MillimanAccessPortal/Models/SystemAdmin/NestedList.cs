/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide a basic data structure that defines a tree-like nested list.
 * DEVELOPER NOTES:
 *      This class is useful as it provides a simple interface to the front end for rendering
 *      nested lists.
 */

using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class NestedList
    {
        public List<NestedListSection> Sections { get; set; } = new List<NestedListSection>();
    }

    public class NestedListSection
    {
        public string Name { get; set; }
        public bool Marked { get; set; }
        public Guid Id { get; set; }
        public List<string> Values { get; set; } = new List<string>();
    }
}
