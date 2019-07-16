using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MillimanAccessPortal.Utilities
{
    public class FileSystemTasks
    {
        public FileSystemTasks()
        {
        }

        /// <summary>
        /// Delete files related to a root content item.
        /// After deleting the file, the related reference is removed from the root content item record.
        /// The calling code is responsible for saving changes to the contentItem.
        /// </summary>
        /// <param name="contentItem">Root content item from which files are to be removed, must be already tracked in EF cache.</param>
        /// <param name="relatedFiles">Related files to be removed</param>
        public void DeleteRelatedFiles(RootContentItem contentItem, List<ContentRelatedFile> relatedFiles)
        {
            var relatedFilesObj = contentItem.ContentFilesList;
            foreach (var relatedFile in relatedFiles.Where(f => f.FilePurpose != "MasterContent"))
            {
                // Delete the file
                FileSystemUtil.DeleteFileWithRetry(relatedFile.FullPath);

                // Update content list in root content item
                relatedFilesObj.RemoveAll(f => f.Id == relatedFile.Id);
            }
            contentItem.ContentFilesList = relatedFilesObj;
        }
    }
}
