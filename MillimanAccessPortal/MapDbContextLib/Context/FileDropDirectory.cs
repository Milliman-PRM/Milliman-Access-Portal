/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a directory in the FileDrop persistence infrastructure
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace MapDbContextLib.Context
{
    public class FileDropDirectory
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Uses syntax convention using '/' as root and directory separator, Root corresponding to the home folder of the containing FileDrop
        /// Can be more conveniently access through [NonMapped] properties
        /// </summary>
        [Required]
        public string CanonicalFileDropPath { get; set; }
        [NotMapped]
        public string FileDropPath
        {
            get => CanonicalFileDropPath;
            set => CanonicalFileDropPath = ConvertPathToCanonicalPath(value);
        }

        /// <summary>
        /// Imposes syntax rules to a path string to achieve repeatable encoding practices.  A canonical path follows syntax of the SFTP protocol, using '/' for root and path separator
        /// </summary>
        /// <param name="path">Must start with a single '/' or '\', representing the home directory of a FileDrop.  A volume name is not supported since that is transparent to the SFTP protocol.</param>
        /// <returns></returns>
        public static string ConvertPathToCanonicalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            if (path.StartsWith("\\\\") ||
                path.StartsWith("//") ||
                (!path.StartsWith("/") && !path.StartsWith("\\")))
            {
                return null;
            }
            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                return null;
            }

            return path.Replace('\\', '/');
        }

        public string Description { get; set; }

        [ForeignKey("ParentDirectory")]
        public Guid? ParentDirectoryId { get; set; }
        public FileDropDirectory ParentDirectory { get; set; }

        [ForeignKey("FileDrop")]
        public Guid FileDropId { get; set; }
        public FileDrop FileDrop { get; set; }

        public virtual ICollection<FileDropDirectory> ChildDirectories { get; set; }
        public virtual ICollection<FileDropFile> Files { get; set; }
    }
}
