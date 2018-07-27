using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientDetailForProfitCenter
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public Dictionary<string, List<string>> AuthorizedUsers { get; set; }
    }
}
