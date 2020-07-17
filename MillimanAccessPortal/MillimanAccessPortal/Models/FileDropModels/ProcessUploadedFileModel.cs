/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Request model supporting the request to process an uploaded file
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class ProcessUploadedFileModel
    {
        public Guid FileUploadId { get; set; }
        public Guid FileDropId { get; set; }
        public Guid FileDropDirectoryId { get; set; }
        public string FileName { get; set; }
    }
}
