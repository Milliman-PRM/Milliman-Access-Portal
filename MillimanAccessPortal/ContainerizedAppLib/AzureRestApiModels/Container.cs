using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class Container
    {
        // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#container

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ContainerProperties Properties { get; set; }
    }
}
