using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDrop
{
    public class ClientsResponseModel
    {
        public Dictionary<Guid, BasicClientWithEligibleUsers> Clients { get; set; }

        /// <summary>
        /// Clients that have children the user can access but cannot be accessed themselves
        /// </summary>
        public Dictionary<Guid, BasicClientWithCardStats> ParentClients { get; set; }
        
        /// <summary>
        /// Users who are eligible in the above clients
        /// </summary>
        public Dictionary<Guid, BasicUser> Users { get; set; }
    }
}
