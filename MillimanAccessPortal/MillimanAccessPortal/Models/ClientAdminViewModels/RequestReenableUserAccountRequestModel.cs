/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Request model for the ClientAdminController.RequestReenableUserAccount action
 * DEVELOPER NOTES:
 */

using AuditLogLib.Event;
using MapCommonLib.ActionFilters;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class RequestReenableUserAccountRequestModel
    {
        /// <summary>
        /// ID of the Client that the requesting Client Admin is an administrator for.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid ClientId { get; set; }

        /// <summary>
        /// ID of the user whose account needs to be reenabled.
        /// </summary>
        [EmitBeforeAfterLog]
        public Guid UserId { get; set; }


        /// <summary>
        /// Reason that Client Admin is requesting a disabled account be re-enabled.
        /// </summary>
        [EmitBeforeAfterLog]
        public ReenableDisabledAccountReason Reason { get; set; }
    }
}