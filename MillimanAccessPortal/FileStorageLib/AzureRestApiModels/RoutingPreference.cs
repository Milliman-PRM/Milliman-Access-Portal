using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#routingpreference
    public class RoutingPreference
    {
        [JsonProperty(PropertyName = "publishInternetEndpoints")]
        public bool PublishInternetEndpoints { get; set; }

        [JsonProperty(PropertyName = "publishMicrosoftEndpoints")]
        public bool PublishMicrosoftEndpoints { get; set; }

        [JsonProperty(PropertyName = "routingChoice")]
        public RoutingChoiceEnum RoutingChoice { get; set; }
    }
}
