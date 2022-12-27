using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#action
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VirtualNetworkRuleActionEnum
    {
        Allow,
    }
}
