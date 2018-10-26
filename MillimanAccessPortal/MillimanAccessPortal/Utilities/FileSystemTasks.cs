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
        /// 
        /// This function is meant to delete simple supporting files. After deleting the file, no additional
        /// cleanup is performed aside from removing its reference from the root content item record.
        /// 
        /// The calling code is responsible for saving changes to contentItem.
        /// </summary>
        /// <param name="contentItem">Root content item from which files are to be removed</param>
        /// <param name="relatedFiles">Related files to be removed</param>
        public void DeleteRelatedFiles(RootContentItem contentItem, List<ContentRelatedFile> relatedFiles)
        {
            var relatedFilesObj = contentItem.ContentFilesList;
            foreach (var relatedFile in relatedFiles.Where(f => f.FilePurpose != "MasterContent"))
            {
                string workingDirectory = Path.GetDirectoryName(relatedFile.FullPath);
                string targetFilePrefix = ContentTypeSpecificApiBase.GenerateContentFileName(
                    relatedFile.FilePurpose, "", contentItem.Id);

                // Assumes only one file for each purpose
                var targetFile = Directory.EnumerateFiles(workingDirectory)
                    .SingleOrDefault(f => Path.GetFileNameWithoutExtension(f) == targetFilePrefix);

                if (targetFile != null)
                {
                    // Delete the file
                    FileSystemUtil.DeleteFileWithRetry(targetFile);

                    // Update content list in root content item
                    relatedFilesObj.RemoveAll(f => f.FilePurpose == relatedFile.FilePurpose);
                }
            }
            contentItem.ContentFilesList = relatedFilesObj;
        }
    }
}
