using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#ipaddress

    internal class IpAddress
    {
        [JsonProperty(PropertyName = "dnsNameLabel")]
        public string DnsNameLabel { get; set; }

        [JsonProperty(PropertyName = "fqdn")]
        public string Fqdn { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string Ip { get; set; }

        [JsonProperty(PropertyName = "ports")]
        public List<ContainerPort> Ports { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = "Public"; // Can also be set to "Private"
    }
}
