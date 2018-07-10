/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishRequest
    {
        public long RootContentItemId { get; set; }

        [Required]
        public UploadedRelatedFile[] RelatedFiles { get; set; }
    }

    public class UploadedRelatedFile
    {
        /// <summary>
        /// Standard values: MasterContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }

        public Guid FileUploadId { get; set; }
    }
}
