/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a file stored in the FileDrop persistence infrastructure
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class FileDropFile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FileName { get; set; }

        public string Description { get; set; }

        public long Size { get; set; } = 0;

        public DateTime? UploadDateTimeUtc { get; set; } = null;

        public string CreatedByAccountUserName { get; set; }

        [ForeignKey("Directory")]
        public Guid DirectoryId { get; set; }
        public FileDropDirectory Directory { get; set; }
    }
}
