/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents the properties of a FileDrop entity that will be stored in the audit log
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;

namespace AuditLogLib.Models
{
    public class FileDropLogModel
    {
        public Guid Id;
        public string Name;
        public string Description;
        public string RootPath;

        public static explicit operator FileDropLogModel(FileDrop fileDrop)
        {
            var x = new FileDropLogModel
            {
                Id = fileDrop.Id,
                Name = fileDrop.Name,
                Description = fileDrop.Description,
                RootPath = fileDrop.RootPath,
            };

            return x;
        }
    }
}
