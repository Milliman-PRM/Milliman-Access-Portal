using MillimanAccessPortal.Models.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ClientsViewModel
    {
        public List<BasicClientWithEligibleUsers> Clients { get; set; }
        public List<BasicUser> Users { get; set; }
    }
}
