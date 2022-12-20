using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#rootsquashtype
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RootSquashTypeEnum
    {
        AllSquash,
        NoRootSquash,
        RootSquash,
    }
}
