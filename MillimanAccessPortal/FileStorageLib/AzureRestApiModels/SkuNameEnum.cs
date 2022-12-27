using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#skuname
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SkuNameEnum
    {
        Premium_LRS,
        Premium_ZRS,
        Standard_GRS,
        Standard_GZRS,
        Standard_LRS,
        Standard_RAGRS,
        Standard_RAGZRS,
        Standard_ZRS,
    }
}
