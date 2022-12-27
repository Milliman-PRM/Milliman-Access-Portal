using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#networkruleset
    public class NetworkRuleSet
    {
        [JsonProperty(PropertyName = "bypass")]
        public TrafficBypassEnum Bypass { get; set; } = TrafficBypassEnum.AzureServices;

        [JsonProperty(PropertyName = "defaultAction")]
        public DefaultActionEnum DefaultAction = DefaultActionEnum.Allow;

        [JsonProperty(PropertyName = "ipRules")]
        public List<IPRule> IPRules { get; set; }

        [JsonProperty(PropertyName = "resourceAccessRules")]
        public List<ResourceAccessRule> ResourceAccessRules { get; set; }

        [JsonProperty(PropertyName = "virtualNetworkRules")]
        public List<VirtualNetworkRule> VirtualNetworkRules { get; set; }
    }
}
