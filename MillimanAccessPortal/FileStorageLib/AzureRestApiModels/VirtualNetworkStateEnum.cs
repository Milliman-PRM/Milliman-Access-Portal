using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#state
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VirtualNetworkStateEnum
    {
        Deprovisioning,
        Failed,
        NetworkSourceDeleted,
        Provisioning,
        Succeeded,
    }
}
