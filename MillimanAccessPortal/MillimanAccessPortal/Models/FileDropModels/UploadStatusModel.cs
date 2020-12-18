/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents information describing the status of a file upload to a file drop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using MillimanAccessPortal.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class UploadStatusModel
    {
        public FileDropUploadTaskStatus Status { get; set; } = FileDropUploadTaskStatus.Unknown;

        public string FileName { get; set; } = string.Empty;
    }
}
