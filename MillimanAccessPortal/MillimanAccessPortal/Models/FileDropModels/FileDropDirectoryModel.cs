/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Model class representing properties of a directory that are needed by front end code
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class FileDropDirectoryModel
    {
        public Guid Id { get; set; }

        public string CanonicalPath { get; set; }

        public string Description { get; set; }

        public FileDropDirectoryModel(FileDropDirectory source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", "");
            }

            Id = source.Id;
            CanonicalPath = source.CanonicalFileDropPath;
            Description = source.Description;
        }
    }
}
