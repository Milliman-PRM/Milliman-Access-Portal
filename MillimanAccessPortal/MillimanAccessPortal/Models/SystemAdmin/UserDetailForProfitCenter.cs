using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserDetailForProfitCenter
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Dictionary<string, List<string>> AssignedClients { get; set; }
    }
}
