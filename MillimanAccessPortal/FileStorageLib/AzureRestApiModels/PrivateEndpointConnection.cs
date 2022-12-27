using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#privateendpointconnection
    public class PrivateEndpointConnection
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public PrivateEndpointConnectionProperties Properties { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
