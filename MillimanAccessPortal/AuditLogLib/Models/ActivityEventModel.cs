/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Contains fields that are returned to users who request bulk FileDrop activity logs
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;

namespace AuditLogLib.Models
{
    public class ActivityEventModel
    {
        public DateTime TimeStampUtc { get; set; }

        public int EventCode { get; set; }

        public string EventType { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public string Description { get; set; }

        public object EventData { get; set; }

        public static ActivityEventModel Generate(AuditEvent evt, Names names)
        {
            return new ActivityEventModel
            {
                TimeStampUtc = evt.TimeStampUtc,
                EventCode = evt.EventCode,
                EventType = evt.EventType,
                UserName = evt.User,
                FullName = $"{names.FirstName} {names.LastName}",
                Description = GenerateDescription(evt),
                EventData = evt.EventDataObject,
            };
        }

        private static string GenerateDescription(AuditEvent evt)
        {
            string descriptionString = string.Empty;

            object eventData = evt.EventDataObject;

            // Properties common to multiple events
            FileDropLogModel fileDropModel = GetNamedPropertyOfSpecifiedType<FileDropLogModel>(eventData, "FileDrop");
            FileDropPermissionGroupLogModel permissionGroupModel = GetNamedPropertyOfSpecifiedType<FileDropPermissionGroupLogModel>(eventData, "PermissionGroup");
            FileDropDirectoryLogModel FileDropDirectoryModel = GetNamedPropertyOfSpecifiedType<FileDropDirectoryLogModel>(eventData, "FileDropDirectory");
            dynamic clientModel = GetNamedPropertyOfSpecifiedType<ExpandoObject>(eventData, "Client");
            dynamic mapUser = GetNamedPropertyOfSpecifiedType<ExpandoObject>(eventData, "MapUser");

            switch (evt.EventCode)
            {
                case 8001:  // File Drop Created
                    descriptionString = $"File drop name is \"{fileDropModel?.Name}\"";
                    if (!string.IsNullOrEmpty(fileDropModel?.Description))
                    {
                        descriptionString += $" and description is \"{fileDropModel.Description}\"";
                    }
                    return descriptionString;

                case 8002:  // File Drop Deleted
                    descriptionString = $"File drop name is \"{fileDropModel?.Name}\"";
                    if (!string.IsNullOrEmpty(fileDropModel?.Description))
                    {
                        descriptionString += $" and description is \"{fileDropModel.Description}\"";
                    }
                    return descriptionString;

                case 8003:  // File Drop Updated
                    FileDropLogModel oldfileDrop = GetNamedPropertyOfSpecifiedType<FileDropLogModel>(eventData, "OldFileDrop");
                    FileDropLogModel newfileDrop = GetNamedPropertyOfSpecifiedType<FileDropLogModel>(eventData, "NewFileDrop");
                    if (oldfileDrop.Name != newfileDrop.Name)
                    {
                        descriptionString += $"File drop name changed from \"{oldfileDrop.Name}\" to \"{newfileDrop.Name}\". ";
                    }
                    if (oldfileDrop.Description != newfileDrop.Description)
                    {
                        descriptionString += $"File drop description changed from \"{oldfileDrop.Description}\" to \"{newfileDrop.Description}\".";
                    }
                    return descriptionString;

                case 8011:  // File Drop Permission Group Created
                    descriptionString += permissionGroupModel.IsPersonalGroup
                                         ? $"Personal permission group created for \"{permissionGroupModel.Name}\""
                                         : $"Permission group name is \"{permissionGroupModel.Name}\"";
                    return descriptionString;

                case 8012:  // File Drop Permission Group Deleted
                    descriptionString = permissionGroupModel.IsPersonalGroup
                                        ? $"Personal permission group \"{permissionGroupModel.Name}\" deleted"
                                        : $"Permission group \"{permissionGroupModel.Name}\" deleted";
                    return descriptionString;

                case 8013:  // Permission Group Updated
                    dynamic oldSettings = GetNamedPropertyOfSpecifiedType<ExpandoObject>(eventData, "PreviousProperties");
                    dynamic newSettings = GetNamedPropertyOfSpecifiedType<ExpandoObject>(eventData, "UpdatedProperties");
                    if (newSettings.Name != oldSettings.Name)
                    {
                        descriptionString += $"Name changed from \"{oldSettings.Name}\" to \"{newSettings.Name}\". ";
                    }

                    if (newSettings.ReadAccess && !oldSettings.ReadAccess)
                    {
                        descriptionString += $"Read access granted. ";
                    }
                    if (newSettings.WriteAccess && !oldSettings.WriteAccess)
                    {
                        descriptionString += $"Write access granted. ";
                    }
                    if (newSettings.DeleteAccess && !oldSettings.DeleteAccess)
                    {
                        descriptionString += $"Delete access granted. ";
                    }

                    if (!newSettings.ReadAccess && oldSettings.ReadAccess)
                    {
                        descriptionString += $"Read access revoked. ";
                    }
                    if (!newSettings.WriteAccess && oldSettings.WriteAccess)
                    {
                        descriptionString += $"Write access revoked. ";
                    }
                    if (!newSettings.DeleteAccess && oldSettings.DeleteAccess)
                    {
                        descriptionString += $"Delete access revoked. ";
                    }
                    return descriptionString;

                case 8100:  // SFTP Account Created
                    descriptionString += $"SFTP account created for MAP user \"{mapUser?.UserName}\". ";
                    return descriptionString;

                case 8101:  // SFTP Account Deleted
                    descriptionString += $"SFTP account deleted for MAP user \"{mapUser?.UserName}\". ";
                    return descriptionString;

                case 8102:  // Account Added To Permission Group
                    descriptionString += $"\"{mapUser?.UserName}\" assigned to permission group \"{permissionGroupModel.Name}\"";
                    return descriptionString;

                case 8103:  // Account Removed From Permission Group
                    descriptionString += $"\"{mapUser?.UserName}\" removed from permission group \"{permissionGroupModel.Name}\"";
                    return descriptionString;

                case 8110:  // SFTP Directory Created
                    descriptionString += $"Directory created: \"{FileDropDirectoryModel.CanonicalFileDropPath}\"";
                    return descriptionString;

                case 8111:  // SFTP Directory Removed
                    descriptionString += $"Directory removed: \"{FileDropDirectoryModel.CanonicalFileDropPath}\"";
                    return descriptionString;

                case 8112:  // SFTP File Write Authorized
                    descriptionString += $"File \"{GetNamedPropertyOfSpecifiedType<string>(eventData, "FileName")}\" written to \"{FileDropDirectoryModel.CanonicalFileDropPath}\"";
                    return descriptionString;

                case 8113:  // SFTP File Read Authorized
                    descriptionString += $"\"{GetNamedPropertyOfSpecifiedType<string>(eventData, "FileName")}\" downloaded from \"{FileDropDirectoryModel.CanonicalFileDropPath}\"";
                    return descriptionString;

                case 8114:  // SFTP File Delete Authorized
                    descriptionString += $"\"{GetNamedPropertyOfSpecifiedType<string>(eventData, "FileName")}\" deleted from \"{FileDropDirectoryModel.CanonicalFileDropPath}\"";
                    return descriptionString;

                case 8115:  // SFTP File Or Directory Renamed
                    descriptionString += $"{GetNamedPropertyOfSpecifiedType<string>(eventData, "Type")} \"{GetNamedPropertyOfSpecifiedType<string>(eventData, "From")}\" renamed to \"{GetNamedPropertyOfSpecifiedType<string>(eventData, "To")}\"";
                    return descriptionString;

                default:
                    return descriptionString;
            }
        }

        /// <summary>
        /// Searches in obj for a property with name propertyName and type T.  Works with C# and Newtonsoft.Json.Linq objects. 
        /// To find a property of undeclared (anonymous) type, call with generic type <see cref="ExpandoObject"/> and  assign the returned value to a dymanic variable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to be reflected</param>
        /// <param name="propertyName">Name of the property to be found in obj</param>
        /// <returns>The found named property or null</returns>
        private static T GetNamedPropertyOfSpecifiedType<T>(object obj, string propertyName) 
            where T : class
        {
            switch (obj)
            {
                case JObject jobj when jobj is JObject:
                    JToken prop = jobj[propertyName];
                    return prop?.ToObject<T>();

                default:
                    var propInfo = obj.GetType().GetProperty(propertyName, typeof(T));
                    return propInfo?.GetValue(obj) as T;
            }
        }

        public class Names
        {
            public string UserName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;

            public static Names Empty => new Names();
        }
    }
}
