using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile
    {
        public string FullPath { get; set; }
        public string FilePurpose { get; set; }
        public string Checksum { get; set; }
    }
}
