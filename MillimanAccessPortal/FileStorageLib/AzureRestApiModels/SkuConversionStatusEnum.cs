using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#skuconversionstatus
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SkuConversionStatusEnum
    {
        Failed,
        InProgress,
        Succeeded,
    }
}
