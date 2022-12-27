using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#minimumtlsversion
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MinimumTlsVersionEnum
    {
        TLS1_0,
        TLS1_1,
        TLS1_2,
    }
}
