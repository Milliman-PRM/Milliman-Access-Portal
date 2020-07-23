using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class SetUserRoleInClientResponseModel
    {
        /// <summary>
        /// ID of the user belonging to the client.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid UserId { get; set; }

        /// <summary>
        /// Roles assigned to the user.
        /// </summary>
        [EmitBeforeAfterLog]
        public List<AssignedRoleInfo> Roles { get; set; }
    }
}