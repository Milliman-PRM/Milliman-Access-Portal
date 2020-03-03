/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents the recursive inventory of all directories and files contained in a single directory (inclusive)
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace AuditLogLib.Models
{
    public class FileDropDirectoryInventoryModel
    {
        public List<FileDropDirectoryLogModel> Directories { get; set; }

        public List<FileDropFile> Files { get; set; }
    }
}
