/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Request model for action to create a new folder in a file drop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class CreateFileDropFolderRequestModel
    {
        public Guid FileDropId { get; set; }
        public Guid ContainingFileDropDirectoryId { get; set; }
        public string NewFolderName { get; set; }
        public string Description { get; set; }
    }
}
