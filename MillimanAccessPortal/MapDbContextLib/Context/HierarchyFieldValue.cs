/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class HierarchyFieldValue
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Value { get; set; }

        [ForeignKey("HierarchyField")]
        public Guid HierarchyFieldId { get; set; }
        public HierarchyField HierarchyField { get; set; }

    }
}
