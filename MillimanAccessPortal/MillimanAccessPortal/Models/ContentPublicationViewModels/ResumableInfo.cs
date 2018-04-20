/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: ViewModel for data commonly sent by resumable.js
 * DEVELOPER NOTES:
 *      Must match data sent by resumable.js, which is defined in src/ts/lib-options.ts
 */

using System.IO;

namespace MillimanAccessPortal.Models.ContentPublicationViewModels
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
        /// Get the expected size of the chunk indicated by this info
        /// </summary>
        /// <param name="resumableInfo">The chunk info to check</param>
        /// <returns>The expected size in bytes of the chunk</returns>
        public static ulong ExpectedSize(this ResumableInfo resumableInfo)
        {
            return resumableInfo.IsLastChunk()
                ? (resumableInfo.TotalSize % resumableInfo.ChunkSize) + resumableInfo.ChunkSize
                : resumableInfo.ChunkSize;
        }
    }
}
