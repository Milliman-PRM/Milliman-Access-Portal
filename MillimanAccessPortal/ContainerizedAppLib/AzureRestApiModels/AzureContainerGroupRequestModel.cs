using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#request-body
    internal class AzureContainerGroupRequestModel
    {
        [JsonProperty(PropertyName = "properties")]
        public ContainerGroupProperties Properties { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        
        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }
        
        [JsonProperty(PropertyName = "zones")]
        public List<string> Zones { get; set; }

        // TODO identity
    }


}
