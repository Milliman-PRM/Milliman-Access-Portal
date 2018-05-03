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
    public class FileUpload
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Checksum { get; set; }

        [Required]
        public DateTime CreatedDateTimeUtc { get; set; }

        [Required]
        public string ClientFileIdentifier { get; set; }

        [Required]
        public string StoragePath { get; set; }
    }
}
