using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#privateendpointconnection
    public class PrivateEndpointConnectionProperties
    {
        [JsonProperty(PropertyName = "privateEndpoint")]
        public PrivateEndpoint PrivateEndpoint { get; set; }

        [JsonProperty(PropertyName = "privateLinkServiceConnectionState")]
        public PrivateLinkServiceConnectionState PrivateLinkServiceConnectionState { get; set; }

        [JsonProperty(PropertyName = "provisioningState")]
        public PrivateEndpointConnectionProvisioningStateEnum ProvisioningState { get; set; }
    }
}
