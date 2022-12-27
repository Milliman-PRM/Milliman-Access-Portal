using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#virtualnetworkrule
    public class VirtualNetworkRule
    {
        [JsonProperty(PropertyName = "action")]
        public VirtualNetworkRuleActionEnum Action { get; set; } = VirtualNetworkRuleActionEnum.Allow;

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "state")]
        public VirtualNetworkStateEnum State { get; set; }
    }
}
