/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A POCO class representing updates to a user's account settings for a particular file drop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class UpdateAccountSettingsModel
    {
        public Guid FileDropId { get; set; }

        public List<UpdateNotificationModel> Notifications { get; set; }
    }

    public class UpdateNotificationModel
    {
        public FileDropNotificationType NotificationType { get; set; }
        public bool IsEnabled { get; set; } = false;
    }

}
