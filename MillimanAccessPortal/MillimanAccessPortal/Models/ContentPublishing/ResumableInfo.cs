/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: ViewModel for data commonly sent by resumable.js
 * DEVELOPER NOTES:
 *      Must match data sent by resumable.js, which is defined in src/ts/lib-options.ts
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class ResumableInfo
    {
        public uint ChunkNumber { get; set; }
        public uint TotalChunks { get; set; }
        public uint ChunkSize { get; set; }
        public ulong TotalSize { get; set; }
        public string FileName { get; set; }
        public string FileExt { get => Path.GetExtension(FileName); }
        public string UID { get; set; }
        public string Checksum { get; set; }
        public string Type { get; set; }
    }

    public static class ResumableExtensions
    {
        /// <summary>
        /// Check if the chunk indicated by this info is the last chunk
        /// </summary>
        /// <param name="resumableInfo">The chunk info to check</param>
        /// <returns>True if the chunk is the last chunk; false otherwise</returns>
        public static bool IsLastChunk(this ResumableInfo resumableInfo)
        {
            return resumableInfo.ChunkNumber == resumableInfo.TotalChunks;
        }

        /// <summary>
        /// Check if the chunk indicated by this info is smaller than the chunk size
        /// </summary>
        /// <param name="resumableInfo">The chunk info to check</param>
        /// <returns>True if the file size is smaller than chunk size; false otherwise</returns>
        public static bool IsSmall(this ResumableInfo resumableInfo)
        {
            return resumableInfo.TotalSize < resumableInfo.ChunkSize;
        }

        /// <summary>
        /// Get the expected size of the chunk indicated by this info
        /// </summary>
        /// <param name="resumableInfo">The chunk info to check</param>
        /// <returns>The expected size in bytes of the chunk</returns>
        public static ulong ExpectedSize(this ResumableInfo resumableInfo)
        {
            if (resumableInfo.IsSmall())
            {
                return resumableInfo.TotalSize;
            }
            else if (resumableInfo.IsLastChunk())
            {
                return resumableInfo.TotalSize % resumableInfo.ChunkSize + resumableInfo.ChunkSize;
            }
            else
            {
                return resumableInfo.ChunkSize;
            }
        }

        public static bool ExtensionIsAcceptable(this ResumableInfo resumableInfo)
        {
            var acceptableExtensions = new List<string>
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".pdf",
                ".qvw",
            };
            return acceptableExtensions.Contains(resumableInfo.FileExt.ToLower());
        }


        /// <summary>
        /// Check the extension of the upload indicated by this info against the initial bytes of the provided file stream
        /// </summary>
        /// <param name="resumableInfo">The chunk info to check</param>
        /// <param name="fileStream">The file stream whose initial bytes to check</param>
        /// <param name="initialByteCount">The number of bytes to read from the file stream</param>
        /// <returns>Whether the initial bytes agree with the file extension</returns>
        public static bool MatchesInitialBytes(this ResumableInfo resumableInfo, FileStream fileStream, int initialByteCount = 0x10)
        {
            // Read the initial bytes from the file stream
            byte[] initialBytes = new byte[initialByteCount];
            fileStream.Read(initialBytes, 0, initialByteCount);

            // Represent acceptable starting byte sequences as a list of byte arrays
            // Expected initial byte sequences referenced from: https://mimesniff.spec.whatwg.org/#matching-a-mime-type-pattern
            List<byte[]> expectedInitialBytes = new List<byte[]>();
            switch(resumableInfo.FileExt.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    expectedInitialBytes.Add(new byte[] { 0xFF, 0xD8, 0xFF });
                    break;
                case ".png":
                    expectedInitialBytes.Add(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
                    break;
                case ".gif":
                    expectedInitialBytes.Add(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 });
                    expectedInitialBytes.Add(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });
                    break;
                case ".pdf":
                    expectedInitialBytes.Add(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });
                    break;
                case ".qvw":
                    expectedInitialBytes.Add(new byte[] { 0x70, 0x17 });
                    break;
                default:
                    break;
            }

            return expectedInitialBytes.Any((byteSequence) =>
            {
                var initialBytesTrimmed = initialBytes.Take(byteSequence.Count());
                return initialBytesTrimmed.SequenceEqual(byteSequence);
            });
        }

    }
}
