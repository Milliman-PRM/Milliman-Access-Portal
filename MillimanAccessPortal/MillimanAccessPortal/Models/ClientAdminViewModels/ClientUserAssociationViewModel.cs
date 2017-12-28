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
        public long ClientId { get; set; }
        public long UserId { get; set; }
    }
}
