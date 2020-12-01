/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Request model supporting the request to update an existing folder's name or description.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
  public class UpdateFileDropFolderRequestModel
  {
    public Guid FileDropId { get; set; }
    public Guid FileId { get; set; }
    public string FolderName { get; set; }
    public string FolderDescription { get; set; }
  }
}
