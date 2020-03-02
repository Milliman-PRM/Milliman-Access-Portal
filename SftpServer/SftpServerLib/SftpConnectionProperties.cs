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

        public Guid? SftpAccountId { get; set; } = null;
        public string SftpAccountName { get; set; } = null;

        public Guid? MapUserId { get; set; } = null;
        public string MapUserName { get; set; } = null;

        public Guid? FileDropId { get; set; } = null;
        public string FileDropName { get; set; } = null;

        public Guid? ClientId { get; set; } = null;
        public string ClientName { get; set; } = null;

        public string FileDropRootPathAbsolute { get; set; } = null;

        public bool ReadAccess { get; set; } = false;
        public bool WriteAccess { get; set; } = false;
        public bool DeleteAccess { get; set; } = false;
    }
}
