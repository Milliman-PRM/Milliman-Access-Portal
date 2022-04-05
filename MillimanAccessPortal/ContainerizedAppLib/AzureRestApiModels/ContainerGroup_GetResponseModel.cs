using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroup_GetResponseModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        // TODO
        // [JsonProperty(PropertyName = "identity")]
        // public ContainerGroupIdentity Identity { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ContainerGroupProperties Properties { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "zones")]
        public List<string> Zones { get; set; }
    }
}
