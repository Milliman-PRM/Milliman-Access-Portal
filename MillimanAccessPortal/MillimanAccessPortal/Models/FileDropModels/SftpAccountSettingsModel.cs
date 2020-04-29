/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents SFTP account settings model properties used for the Account Settings tab in the UI
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public HashSet<NotificationModel> Notifications { get; set; } = new HashSet<NotificationModel>(new NotificationModelEqualityComparer());
    }

    public enum FileDropNotificationType
    {
        FileWritten,
    }

    public class NotificationModel
    {
        public FileDropNotificationType NotificationType { get; set; }
        public bool IsEnabled { get; set; } = false;
        public bool CanModify { get; set; } = false;
    }

    class NotificationModelEqualityComparer : IEqualityComparer<NotificationModel>
    {
        public bool Equals(NotificationModel m1, NotificationModel m2)
        {
            if (m2 == null && m1 == null)
                return true;
            else if (m1 == null || m2 == null)
                return false;
            else if (m1.NotificationType == m2.NotificationType)
                return true;
            else
                return false;
        }

        public int GetHashCode(NotificationModel n)
        {
            return n.NotificationType.GetHashCode();
        }
    }
}
