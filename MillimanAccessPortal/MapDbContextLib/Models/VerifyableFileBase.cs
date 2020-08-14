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
    public class VerifyableFileBase
    {
        protected string _Checksum = null;
        protected string _FullPath { get; set; }

        /// <summary>
        /// Validates the bytes of a file agains the stored checksum property of this ContentRelatedFile instance
        /// </summary>
        /// <returns>true if the stored checksum matches the file content</returns>
        public bool ValidateChecksum()
        {
            (string fileChecksum, long length) = GlobalFunctions.GetFileChecksum(_FullPath);

            if (_Checksum.Equals(fileChecksum, StringComparison.OrdinalIgnoreCase))
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
