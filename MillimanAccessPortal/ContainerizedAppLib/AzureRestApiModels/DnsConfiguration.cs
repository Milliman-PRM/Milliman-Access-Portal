using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class DnsConfiguration
    {
        [JsonProperty(PropertyName = "nameServers")]
        public List<string> NameServers { get; set; }

        [JsonProperty(PropertyName = "options")]
        public string Options { get; set; }

        [JsonProperty(PropertyName = "searchDomains")]
        public string SearchDomains { get; set; }

    }
}
