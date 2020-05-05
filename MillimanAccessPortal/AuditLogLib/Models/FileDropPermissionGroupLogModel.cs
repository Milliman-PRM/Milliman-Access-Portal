/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents information to be logged about a file drop permission group
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuditLogLib.Models
{
    public class FileDropPermissionGroupLogModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool ReadAccess { get; set; }

        public bool WriteAccess { get; set; }

        public bool DeleteAccess { get; set; }

        public bool IsPersonalGroup { get; set; }

        public static explicit operator FileDropPermissionGroupLogModel(FileDropUserPermissionGroup arg)
        {
            return new FileDropPermissionGroupLogModel
            {
                Id = arg.Id,
                Name = arg.Name,
                ReadAccess = arg.ReadAccess,
                WriteAccess = arg.WriteAccess,
                DeleteAccess = arg.DeleteAccess,
                IsPersonalGroup = arg.IsPersonalGroup,
            };
        }
    }
}
