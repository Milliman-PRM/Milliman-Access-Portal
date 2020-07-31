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

        public string Size { get; set; }

        public DateTime? UploadDateTimeUtc { get; set; }

        public FileDropFileModel(FileDropFile source)
        {
            Id = source.Id;
            FileName = source.FileName;
            Description = source.Description;
            Size = Math.Log(source.Size, 1024) switch
            {
                double e when e < 1 => $"{source.Size}  B",
                double e when e < 2 => $"{source.Size / Math.Pow(1024, 1):F2} kB",
                double e when e < 3 => $"{source.Size / Math.Pow(1024, 2):F2} MB",
                double e when e < 4 => $"{source.Size / Math.Pow(1024, 3):F2} GB",
                _ => $"{source.Size / Math.Pow(1024, 4):F2} TB",
            };
            UploadDateTimeUtc = source.UploadDateTimeUtc;
        }
    }
}
