/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A POCO class representing updates to a user's account settings for a particular file drop
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class UpdateAccountSettingsModel
    {
        public Guid FileDropId { get; set; }

        public List<FileDropUserNotificationModel> Notifications { get; set; }
    }

}
