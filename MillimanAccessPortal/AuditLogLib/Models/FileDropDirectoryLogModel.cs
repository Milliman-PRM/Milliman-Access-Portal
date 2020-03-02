/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A subset of the FileDropDirectory entity class for audit logging purposes
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuditLogLib.Models
{
    public class FileDropDirectoryLogModel
    {
        public Guid Id { get; set; }
        public string CanonicalFileDropPath { get; set; }
        public string Description { get; set; }

        public static explicit operator FileDropDirectoryLogModel(FileDropDirectory dir)
        {
            return new FileDropDirectoryLogModel
            {
                Id = dir.Id,
                Description = dir.Description,
                CanonicalFileDropPath = dir.CanonicalFileDropPath,
            };
        }
    }
}
