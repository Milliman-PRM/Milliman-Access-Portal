using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#enabledprotocols
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnabledProtocolsEnum
    {
        NFS,
        SMB,
    }
}
