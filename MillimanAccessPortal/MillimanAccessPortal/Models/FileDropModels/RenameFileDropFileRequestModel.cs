/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Represents a reqeust to rename/move a file in a File Drop.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
  public class RenameFileDropFileRequestModel
  {
    public Guid FileDropId { get; set; }
    public Guid FileId { get; set; }
    public Guid NewFolderId { get; set; }
    public string FileName { get; set; }
  }
}
