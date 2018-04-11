using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        public string Checksum { get => UID.Split('-').Last(); }
        public string Type { get; set; }
        public long RootContentItemId { get; set; }
    }
}
