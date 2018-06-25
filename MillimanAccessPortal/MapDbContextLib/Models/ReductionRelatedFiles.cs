using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ReductionRelatedFiles
    {
        public ContentRelatedFile MasterContentFile { get; set; }
        public List<ContentRelatedFile> ReducedContentFileList { get; set; } = new List<ContentRelatedFile>();
    }
}
