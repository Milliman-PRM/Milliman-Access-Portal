using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#privatelinkserviceconnectionstate
    public class PrivateLinkServiceConnectionState
    {
        [JsonProperty(PropertyName = "actionRequired")]
        public string ActionRequired { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "status")]
        public PrivateEndpointServiceConnectionStatusEnum Status { get; set; }
    }
}
