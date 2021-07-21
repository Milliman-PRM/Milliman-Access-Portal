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
            try
            {
                return StaticUtil.DoRetryOperationWithReturn<ApplicationException, bool>(
                    () => {
                        (string fileChecksum, long length) = GlobalFunctions.GetFileChecksum(_FullPath);

                        if (_Checksum.Equals(fileChecksum, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        else
                        {
                            ApplicationException newException = new ApplicationException($"Checksums do not match (retryable): file checksum is {fileChecksum}, length is {length}");
                            Log.Warning(newException.Message);
                            // This is what triggers the retry, up to the specified number of times
                            throw newException;
                        }
                    }, 3, 500, true);
            }
            catch (ApplicationException)
            {
                return false;
            }
        }
    }
}
