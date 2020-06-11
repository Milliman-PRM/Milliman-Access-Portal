/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: POCO class for parameters to be passed to the corresponding audit log event
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace AuditLogLib.Models
{
    public class SftpFileOperationLogModel
    {
        public string FileName { get; set; }
        public FileDropDirectoryLogModel FileDropDirectory { get; set; }
        public FileDropLogModel FileDrop { get; set; }
        public SftpAccount Account { get; set; }
        public ApplicationUser User { get; set; }
    }
}
