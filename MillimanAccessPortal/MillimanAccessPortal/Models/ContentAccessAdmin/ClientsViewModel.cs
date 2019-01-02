using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ClientsViewModel
    {
        public Dictionary<Guid, BasicClientWithEligibleUsers> Clients { get; set; }
        public Dictionary<Guid, BasicUser> Users { get; set; }
    }
}
