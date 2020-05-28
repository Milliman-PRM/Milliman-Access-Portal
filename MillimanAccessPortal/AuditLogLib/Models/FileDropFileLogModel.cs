/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model conveying properties of a FileDropFile record to be recorded in the audit log
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace AuditLogLib.Models
{
    public class FileDropFileLogModel
    {
        Guid Id { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }

        public static explicit operator FileDropFileLogModel(FileDropFile file)
        {
            return new FileDropFileLogModel
            {
                Id = file.Id,
                Description = file.Description,
                FileName = file.FileName,
            };
        }
    }
}
