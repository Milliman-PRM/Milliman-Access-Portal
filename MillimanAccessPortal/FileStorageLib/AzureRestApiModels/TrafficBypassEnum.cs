using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#bypass
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TrafficBypassEnum
    {
        AzureServices,
        Logging,
        Metrics,
        None,
    }
}
