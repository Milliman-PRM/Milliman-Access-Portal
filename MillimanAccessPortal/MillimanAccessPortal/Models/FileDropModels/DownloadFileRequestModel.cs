using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class DownloadFileRequestModel
    {
        public Guid FileDropId { get; set; }
        public Guid FileDropFileId { get; set; }
        public string CanonicalFilePath { get; set; }
    }
}
