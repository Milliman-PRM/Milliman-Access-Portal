/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents a request for all accumulated changes to FileDropPermissionGroups
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Models;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class UpdatePermissionGroupsModel
    {
        public Guid FileDropId { get; set; }
        public List<Guid> RemovedPermissionGroupIds { get; set; } = new List<Guid>();
        public List<NewPermissionGroup> NewPermissionGroups { get; set; } = new List<NewPermissionGroup>();
        public Dictionary<Guid, UpdatedPermissionGroup> UpdatedPermissionGroups { get; set; } = new Dictionary<Guid, UpdatedPermissionGroup>();
    }

    public class NewPermissionGroup
    {
        public string Name { get; set; }
        public bool IsPersonalGroup { get; set; }
        public List<NonUserSftpAccount> AssignedSftpAccounts { get; set; } = new List<NonUserSftpAccount>();
        public List<Guid> AssignedMapUserIds { get; set; } = new List<Guid>();
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool DeleteAccess { get; set; }
    }

    public class UpdatedPermissionGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Guid> UsersAdded { get; set; } = new List<Guid>();
        public List<Guid> UsersRemoved { get; set; } = new List<Guid>();
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool DeleteAccess { get; set; }

        /// <summary>
        /// For accounts not associated with MAP users
        /// </summary>
        public List<NonUserSftpAccount> SftpAccountsAdded { get; set; } = new List<NonUserSftpAccount>();

        /// <summary>
        /// For accounts not associated with MAP users
        /// </summary>
        public List<Guid> SftpAccountsRemoved { get; set; } = new List<Guid>();

        public static explicit operator FileDropPermissionGroupLogModel(UpdatedPermissionGroup source)
        {
            return new FileDropPermissionGroupLogModel
            {
                Id = source.Id,
                Name = source.Name,
                ReadAccess = source.ReadAccess,
                WriteAccess = source.WriteAccess,
                DeleteAccess = source.DeleteAccess,
            };
        }
    }

    public class NonUserSftpAccount
    {
        /// <summary>
        /// Use null for a newly created account or an existing account ID to reference an existing record
        /// </summary>
        public Guid? Id { get; set; } = null;

        public string AccountName { get; set; }
        public bool IsSuspended { get; set; }
    }
}
