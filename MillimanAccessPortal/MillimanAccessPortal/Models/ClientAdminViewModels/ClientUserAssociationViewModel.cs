/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing an association between a User and a Client entity
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientUserAssociationViewModel
    {
        public Guid ClientId { get; set; }
        public Guid UserId { get; set; }
    }
}
