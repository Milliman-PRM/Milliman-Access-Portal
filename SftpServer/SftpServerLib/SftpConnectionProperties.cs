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

        public SftpAccount Account { get; set; }

        public ApplicationUser MapUser { get; set; }
        public bool MapUserIsSso { get; set; } = false;

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
