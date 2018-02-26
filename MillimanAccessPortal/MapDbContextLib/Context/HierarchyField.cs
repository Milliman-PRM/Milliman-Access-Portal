/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Models;

namespace MapDbContextLib.Context
{
    public class HierarchyField
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string FieldName { get; set; }

        [Required]
        public string FieldDisplayName { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public FieldStructureType StructureType { get; set; } = FieldStructureType.Unknown;

        public string FieldDelimiter { get; set; } = null;

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

    }
}
