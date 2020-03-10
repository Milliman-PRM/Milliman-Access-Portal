/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace AuditLogLib.Models
{
    public class SftpRenameLogModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool IsDirectory { get; set; }
        public FileDropLogModel FileDrop { get; set; }
        public SftpAccount Account { get; set; }
        public ApplicationUser User { get; set; }
    }
}
