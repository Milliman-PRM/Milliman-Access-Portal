using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#identitytype
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentityTypeEnum
    {
        None,
        SystemAssigned,
        [EnumMember(Value = "SystemAssigned,UserAssigned")]
        SystemAssignedUserAssigned,
        UserAssigned,
    }
}
