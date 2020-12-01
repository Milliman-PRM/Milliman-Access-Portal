/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class UpdateAllUserRolesInClientRequestModel
    {
        /// <summary>
        /// ID of the client which the user belongs to.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid ClientId { get; set; }

        /// <summary>
        /// ID of the user belonging to the client.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid UserId { get; set; }

        /// <summary>
        /// Roles and their requested assignment status
        /// </summary>
        [EmitBeforeAfterLog]
        public List<ClientRoleAssignment> RoleAssignments { get; set; }

        /// <summary>
        /// Reason for the change, to be logged for HITRUST purposes
        /// </summary>
        public int Reason { get; set; }
    }

    public class ClientRoleAssignment
    {
        /// <summary>
        /// RoleEnum of role being assigned or unassigned from the client user.
        /// </summary>
        [EmitBeforeAfterLog]
        public RoleEnum RoleEnum { get; set; }

        /// <summary>
        /// Determines whether or not role should be assigned or unassigned.
        /// </summary>
        [EmitBeforeAfterLog]
        public bool IsAssigned { get; set; }
    }
}
