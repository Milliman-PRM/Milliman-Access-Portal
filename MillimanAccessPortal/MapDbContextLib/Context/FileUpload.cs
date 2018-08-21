/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: An entity representing a file that has been uploaded to the system
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
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

        [NotMapped]
        public bool VirusScanWindowComplete
        {
            get => (CreatedDateTimeUtc + TimeSpan.FromSeconds(GlobalFunctions.virusScanWindowSeconds)) < DateTime.UtcNow;
        }

        [Required]
        public string ClientFileIdentifier { get; set; }

        [Required]
        public string StoragePath { get; set; }
    }
}
