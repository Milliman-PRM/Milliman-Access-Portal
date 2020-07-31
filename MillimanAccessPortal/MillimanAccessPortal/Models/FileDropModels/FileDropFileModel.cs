/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model class representing properties of a file that are needed by front end code
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class FileDropFileModel
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string Description { get; set; }

        public long Size { get; set; }

        public DateTime? UploadDateTimeUtc { get; set; }

        public FileDropFileModel(FileDropFile source)
        {
            Id = source.Id;
            FileName = source.FileName;
            Description = source.Description;
            Size = source.Size;
            UploadDateTimeUtc = source.UploadDateTimeUtc;
        }
    }
}
