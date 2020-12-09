/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Request model supporting the request to update file information, such as description.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
  public class UpdateFileDropFileRequestModel
  {
    public Guid FileDropId { get; set; }
    public Guid FileId { get; set; }
    public string FileDescription { get; set; }
  }
}
