/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents SFTP account settings model properties used for the Account Settings tab in the UI
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class SftpAccountSettingsModel
    {
        public static int DefaultPort => 22;

        public string SftpHost { get; set; } = default;
        public int SftpPort { get; set; } = DefaultPort;
        public string Fingerprint { get; set; } = default;
        public string SftpUserName { get; set; } = default;
        public bool UserHasPassword { get; set; } = default;
        public bool IsSuspended { get; set; } = default;
        public bool IsPasswordExpired { get; set; } = default;
        public Guid? AssignedPermissionGroupId { get; set; } = null;
        public HashSet<NotificationModel> Notifications { get; set; } = new HashSet<NotificationModel>(new FileDropUserNotificationModelSameEventComparer());
    }

    public class NotificationModel : FileDropUserNotificationModel
    {
        public bool CanModify { get; set; } = false;
    }
}
