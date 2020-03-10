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

        public List<Guid> RemovedPermissionGroupIds { get; set; }

        public List<NewPermissionGroup> NewPermissionGroups { get; set; }

        public Dictionary<Guid,UpdatedPermissionGroup> UpdatedPermissionGroups { get; set; }
    }

    public class NewPermissionGroup
    {
        public string Name { get; set; }
        public List<Guid> AuthorizedMapUsers { get; set; } = new List<Guid>();
        public List<Guid> AuthorizedSftpAccounts { get; set; } = new List<Guid>();
        public bool IsPersonalGroup { get; set; }
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
    }
}
