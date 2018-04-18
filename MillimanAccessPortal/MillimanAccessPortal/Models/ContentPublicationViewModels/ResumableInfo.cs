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
}
