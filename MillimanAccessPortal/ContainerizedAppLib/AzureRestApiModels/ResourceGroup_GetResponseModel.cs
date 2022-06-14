using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ResourceGroup_GetResponseModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "managedBy")]
        public string ManagedBy { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ResourceGroupProperties Properties { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
