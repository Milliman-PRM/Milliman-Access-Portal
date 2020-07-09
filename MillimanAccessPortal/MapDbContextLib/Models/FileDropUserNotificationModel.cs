/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A POCO model representing a user's election to receive notification about one FileDrop event type
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Models
{
    public enum FileDropNotificationType
    {
        FileWrite = 0,
        FileRead = 1,
        FileDelete = 2,
    }

    public class FileDropUserNotificationModel
    {
        public FileDropNotificationType NotificationType { get; set; }
        public bool IsEnabled { get; set; } = false;
    }

    public class FileDropUserNotificationModelSameEventComparer : IEqualityComparer<FileDropUserNotificationModel>
    {
        public bool Equals(FileDropUserNotificationModel m1, FileDropUserNotificationModel m2)
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

        public int GetHashCode(FileDropUserNotificationModel n)
        {
            return n.NotificationType.GetHashCode();
        }
    }
}
