/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents the properties of a MAP content related file
 * DEVELOPER NOTES: An associated file is one that is uploaded in the context of a content item and available for access by authorized users of that content item, 
 *                  and that is not one of the standard "related" files (icon, release notes, user guide). 
 */

using MapCommonLib;
using Serilog;
using System;
using System.ComponentModel;

namespace MapDbContextLib.Models
{
    public class ContentAssociatedFile
    {
        [ReadOnly(true)]
        public Guid Id { get; set; } // = Guid.NewGuid();

        public string FullPath { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string FileOriginalName { get; set; } = string.Empty;

        public string SortOrder { get; set; } = string.Empty;

        public ContentAssociatedFileType FileType { get; set; } = ContentAssociatedFileType.Unknown;

        private string _Checksum = null;
        public string Checksum
        {
            get { return _Checksum; }
            set { _Checksum = value?.ToLower(); }
        }


        /// <summary>
        /// Validates the bytes of a file agains the stored checksum property of this ContentAssociatedFile instance
        /// </summary>
        /// <returns>true if the stored checksum matches the file content</returns>
        public bool ValidateChecksum()
        {
            (string fileChecksum, long length) = GlobalFunctions.GetFileChecksum(FullPath);

            if (Checksum.Equals(fileChecksum, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                Log.Warning($"Checksums do not match: file checksum is {fileChecksum}, length is {length}");
                return false;
            }
        }
    }
}
