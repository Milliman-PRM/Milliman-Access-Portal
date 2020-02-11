/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a directory in the FileDrop persistence infrastructure
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class FileDropDirectory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [ForeignKey("ParentDirectoryEntry")]
        public Guid? ParentDirectoryId { get; set; }
        public FileDropDirectory ParentDirectoryEntry { get; set; }

        [ForeignKey("CreatedByAccount")]
        public Guid CreatedByAccountId { get; set; }
        public SftpAccount CreatedByAccount { get; set; }

        [ForeignKey("FileDrop")]
        public Guid FileDropId { get; set; }
        public FileDrop FileDrop { get; set; }

        public virtual ICollection<FileDropDirectory> ChildDirectories { get; set; }
        public virtual ICollection<FileDropFile> Files { get; set; }
    }
}
