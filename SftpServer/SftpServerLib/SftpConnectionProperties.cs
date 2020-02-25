/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SftpServerLib
{
    public class SftpConnectionProperties
    {
        [Key]
        public string Id { get; set; }

        public DateTime OpenedDateTimeUtc { get; set; }

        public DateTime LastActivityUtc { get; set; }

        public Guid SftpAccountId { get; set; }
        public string SftpAccountName { get; set; }

        public Guid? MapUserId { get; set; }
        public string MapUserName { get; set; }

        public Guid FileDropId { get; set; }
        public string FileDropName { get; set; }

        public string FileDropRootPathAbsolute { get; set; }

        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool DeleteAccess { get; set; }
    }
}
