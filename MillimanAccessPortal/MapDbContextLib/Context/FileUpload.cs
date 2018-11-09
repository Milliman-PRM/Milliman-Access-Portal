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

        public FileUploadExtension FileUploadExtension { get; set; }
    }

    public class FileUploadExtension
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Checksum { get; set; }

        [Required]
        public DateTime CreatedDateTimeUtc { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool VirusScanWindowComplete
        {
            get => (CreatedDateTimeUtc + TimeSpan.FromSeconds(GlobalFunctions.virusScanWindowSeconds)) < DateTime.UtcNow;
        }

        [Required]
        public string StoragePath { get; set; }

        [ForeignKey("FileUpload")]
        public Guid FileUploadId { get; set; }
        public FileUpload FileUpload { get; set; }
    }
}
