/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using Serilog;
using System;

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile
    {
        private string _Checksum = null;

        public string FullPath { get; set; }

        /// <summary>
        /// Standard values: MasterContent, ReducedContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }
        public string FileOriginalName { get; set; }
        public string Checksum
        {
            get { return _Checksum; }
            set { _Checksum = value?.ToLower(); }
        }

        /// <summary>
        /// Validates the bytes of a file agains the stored checksum property of this ContentRelatedFile instance
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
