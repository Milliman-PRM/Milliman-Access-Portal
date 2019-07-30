/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    public class ContentAssociatedFileModel
    {
        public Guid Id { get; set; }
        public string Checksum { get; set; }
        public ContentAssociatedFileType FileType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string FileOriginalName { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="source"></param>
        public ContentAssociatedFileModel(ContentAssociatedFile source)
        {
            Id = source.Id;
            Checksum = source.Checksum;
            FileType = source.FileType;
            DisplayName = source.DisplayName;
            FileOriginalName = source.FileOriginalName;
            SortOrder = source.SortOrder;
        }
    }
}
