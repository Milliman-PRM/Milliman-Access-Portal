/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class HierarchyFieldValue
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int HierarchyLevel { get; set; }

        [Required]
        public string Value { get; set; }

        [ForeignKey("ParentValue")]
        public long? ParentHierarchyFieldValueId { get; set; }
        public HierarchyFieldValue ParentValue { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

    }
}
