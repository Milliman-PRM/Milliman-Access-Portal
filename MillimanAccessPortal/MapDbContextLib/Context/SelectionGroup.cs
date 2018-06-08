/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace MapDbContextLib.Context
{
    public class SelectionGroup
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string GroupName { get; set; }

        public string ContentInstanceUrl { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        /// <summary>
        /// This can't be a foreign key due to use of collection type
        /// </summary>
        public long[] SelectedHierarchyFieldValueList { get; set; }

        [Required]
        public bool IsMaster { get; set; }

    }
}
