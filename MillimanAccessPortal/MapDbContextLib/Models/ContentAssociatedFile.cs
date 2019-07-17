/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
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
            return Checksum == GlobalFunctions.GetFileChecksum(FullPath).ToLower();
        }
    }
}
