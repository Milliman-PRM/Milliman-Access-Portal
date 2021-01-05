/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A view model representing the contents of one directory in a FileDrop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class DirectoryContentModel
    {
        public FileDropDirectoryModel ThisDirectory { get; set; }

        public List<FileDropDirectoryModel> Directories { get; set; } = new List<FileDropDirectoryModel>();

        public List<FileDropFileModel> Files { get; set; } = new List<FileDropFileModel>();

        public PermissionSet CurrentUserPermissions { get; set; } = new PermissionSet();
    }
}
