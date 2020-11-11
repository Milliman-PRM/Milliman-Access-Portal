/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
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
        public DateTime ClientAccessReviewDeadline { get; set; }

        public string FileDropRootPathAbsolute { get; set; } = null;

        /// <summary>
        /// Relates the file handle as reported by IP*Works to the corresponding FileDropFile Id
        /// </summary>
        public Dictionary<string,Guid> OpenFileWrites { get; set; } = new Dictionary<string, Guid>();

        public bool ReadAccess { get; set; } = false;
        public bool WriteAccess { get; set; } = false;
        public bool DeleteAccess { get; set; } = false;
    }
}
