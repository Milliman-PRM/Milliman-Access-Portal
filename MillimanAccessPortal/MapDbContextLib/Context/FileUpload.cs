/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: An entity representing an upload and (sometimes) the resulting file
 * DEVELOPER NOTES: 
 */

using MapCommonLib;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public enum FileUploadStatus
    {
        InProgress = 0,
        Complete = 1,
        Error = 2,
    }

    public class FileUpload
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public DateTime InitiatedDateTimeUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public string ClientFileIdentifier { get; set; }

        [Required]
        public FileUploadStatus Status { get; set; } = FileUploadStatus.InProgress;

        public string StatusMessage { get; set; }

        // These members should only be populated if Status = FileUploadStatus.Complete
        public string Checksum { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        // PG does not set datetimes as nullable, instead giving them a default value.
        // This property may give an incorrect answer if CreatedDateTimeUtc has not been set.
        [NotMapped]
        public bool VirusScanWindowComplete
        {
            get => (CreatedDateTimeUtc + TimeSpan.FromSeconds(GlobalFunctions.virusScanWindowSeconds)) < DateTime.UtcNow;
        }

        public string StoragePath { get; set; }
    }
}
