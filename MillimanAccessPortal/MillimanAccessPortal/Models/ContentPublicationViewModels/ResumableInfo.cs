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
        public static bool LastChunk(this ResumableInfo resumableInfo)
        {
            return resumableInfo.ChunkNumber == resumableInfo.TotalChunks;
        }

        public static ulong ExpectedSize(this ResumableInfo resumableInfo)
        {
            return resumableInfo.LastChunk()
                ? (resumableInfo.TotalSize % resumableInfo.ChunkSize) + resumableInfo.ChunkSize
                : resumableInfo.ChunkSize;
        }
    }
}
