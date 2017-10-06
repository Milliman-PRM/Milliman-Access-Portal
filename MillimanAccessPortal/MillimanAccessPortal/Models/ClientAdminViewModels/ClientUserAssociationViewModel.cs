using System;
using System.Collections.Generic;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientUserAssociationViewModel
    {
        public long ClientId { get; set; }
        public string UserName { get; set; }
    }
}
