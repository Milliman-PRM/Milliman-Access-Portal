using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublicationViewModels
{
    public class ResumableData
    {
        public int ChunkNumber { get; set; }
        public int TotalChunks { get; set; }
        public int ChunkSize { get; set; }
        public string UID { get; set; }
        public string Type { get; set; }
        public long RootContentItemId { get; set; }
    }
}
