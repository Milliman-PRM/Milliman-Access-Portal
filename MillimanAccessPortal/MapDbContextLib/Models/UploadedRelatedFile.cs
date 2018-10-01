using System;

namespace MapDbContextLib.Models
{
    public class UploadedRelatedFile
    {
        public string FileOriginalName { get; set; }

        /// <summary>
        /// Standard values: MasterContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }

        public Guid FileUploadId { get; set; }
    }
}
