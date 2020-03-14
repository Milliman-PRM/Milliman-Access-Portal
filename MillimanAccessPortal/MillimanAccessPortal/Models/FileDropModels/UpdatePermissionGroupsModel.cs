/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents a request for all accumulated changes to FileDropPermissionGroups
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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
        public List<Guid> AssignedSftpAccountIds { get; set; }
        public List<Guid> AssignedMapUserIds { get; set; } = new List<Guid>();
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool DeleteAccess { get; set; }

        /// <summary>
        /// For accounts not associated with MAP users
        /// </summary>
        public List<Guid> AuthorizedSftpAccounts { get; set; } = new List<Guid>();
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
        public List<Guid> SftpAccountsAdded { get; set; } = new List<Guid>();
        /// <summary>
        /// For accounts not associated with MAP users
        /// </summary>
        public List<Guid> SftpAccountsRemoved { get; set; } = new List<Guid>();
    }
}
