/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model to represent uploaded content associated file(s) to be published
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MapDbContextLib.Models
{
    public class UploadedAssociatedFile
    {
        public Guid Id { get; set; }

        public string FileOriginalName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string SortOrder { get; set; } = string.Empty;

        public ContentAssociatedFileType FileType { get; set; } = ContentAssociatedFileType.Unknown;
    }
}
