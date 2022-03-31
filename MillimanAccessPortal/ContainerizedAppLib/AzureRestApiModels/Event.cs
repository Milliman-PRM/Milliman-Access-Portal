using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#event
    internal class Event
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "firstTimestamp")]
        public string FirstTimestamp { get; set; }

        [JsonProperty(PropertyName = "lastTimestamp")]
        public string LastTimestamp { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
