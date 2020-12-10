/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents information consumed by the front end code in response to actions related to the permission group tab
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class PermissionGroupsModel
    {
        public Guid FileDropId { get; set; }
        public Dictionary<Guid, EligibleUserModel> EligibleUsers { get; set; } = new Dictionary<Guid, EligibleUserModel>();
        public Dictionary<Guid, PermissionGroupModel> PermissionGroups { get; set; } = new Dictionary<Guid, PermissionGroupModel>();
        public ClientCardModel ClientModel { get; set; }
    }

    public class EligibleUserModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class PermissionGroupModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsPersonalGroup { get; set; }
        public List<Guid> AssignedSftpAccountIds { get; set; }
        public List<Guid> AssignedMapUserIds { get; set; }
        public PermissionSet Permissions { get; set; }
    }
}

