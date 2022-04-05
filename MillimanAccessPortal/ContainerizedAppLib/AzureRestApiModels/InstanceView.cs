using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class InstanceView
    {
        // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/get#instanceview

        [JsonProperty(PropertyName = "events")]
        public List<Event> Events { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }
}
