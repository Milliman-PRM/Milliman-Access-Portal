using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ClientsResponseModel
    {
        public Dictionary<Guid, BasicClientWithEligibleUsers> Clients { get; set; }
        
        /// <summary>
        /// Users who are eligible in the above clients
        /// </summary>
        public Dictionary<Guid, BasicUser> Users { get; set; }
    }
}
