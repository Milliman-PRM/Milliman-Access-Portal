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
                    double e when e < 1 => string.Format("{0,7:###0}  B", source.Size),
                    double e when e < 2 => string.Format("{0,7:###0.00} kB", source.Size / Math.Pow(1024, 1)),
                    double e when e < 3 => string.Format("{0,7:###0.00} MB", source.Size / Math.Pow(1024, 2)),
                    double e when e < 4 => string.Format("{0,7:###0.00} GB", source.Size / Math.Pow(1024, 3)),
                    _ => string.Format("{0,7:###0.00} TB", source.Size / Math.Pow(1024, 4)),
                };
            UploadDateTimeUtc = source.UploadDateTimeUtc;
        }
    }
}
