/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Request model supporting the request to delete a folder within a File Drop.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
  public class RemoveFileDropFolderRequestModel
  {
    public Guid FileDropId { get; set; }
    public Guid FolderId { get; set; }
  }
}
