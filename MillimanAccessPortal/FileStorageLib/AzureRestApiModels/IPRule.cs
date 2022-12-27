using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#iprule
    public class IPRule
    {
        [JsonProperty(PropertyName = "action")]
        public VirtualNetworkRuleActionEnum Action { get; set; } = VirtualNetworkRuleActionEnum.Allow;

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
