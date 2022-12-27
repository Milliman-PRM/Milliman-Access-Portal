using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#defaultaction
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DefaultActionEnum
    {
        Allow,
        Deny,
    }
}
