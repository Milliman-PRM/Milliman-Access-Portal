/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace MapDbContextLib.Context
{
    public class SelectedHierarchyValue
    {
        public long Id { get; set; }

        [ForeignKey("ContentItemUserGroup")]
        public long ContentItemUserGroupId { get; set; }
        public ContentItemUserGroup ContentItemUserGroup { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        /// <summary>
        /// This can't be a foreign key due to use of collection type
        /// </summary>
        public List<HierarchyFieldValue> HierarchyFieldValueList { get; set; }

    }
}
