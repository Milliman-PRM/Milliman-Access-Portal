using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using System;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class SetUserRoleInClientRequestModel
    {
        /// <summary>
        /// ID of the client which the user belongs to.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid ClientId { get; set; }

        /// <summary>
        /// Determines whether or not role should be assigned or unassigned.
        /// </summary>
        [EmitBeforeAfterLog]
        public bool IsAssigned { get; set; }

        /// <summary>
        /// RoleEnum of role being assigned or unassigned from the client user.
        /// </summary>
        [EmitBeforeAfterLog]
        public RoleEnum RoleEnum { get; set; }

        /// <summary>
        /// ID of the user belonging to the client.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid UserId { get; set; }
    }
}
