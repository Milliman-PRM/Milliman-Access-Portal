/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents a reqeust to rename/move a folder in a file drop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class RenameFileDropFolderRequestModel
    {
        public Guid FileDropId { get; set; }
        public Guid DirectoryId { get; set; }
        public string ParentCanonicalPath { get; set; }
        public string DirectoryName { get; set; }
    }
}
