using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerProperties
    {
        [JsonProperty(PropertyName = "command")]
        public List<string> Commands { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "instanceView")]
        public InstanceView Instance_View { get; set; }

        [JsonProperty(PropertyName = "ports")]
        public List<ContainerPort> Ports { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public ResourceRequirements Resources { get; set; }

        // TODO environmentVariables
        // TODO volumeMounts
        // TODO readinessProbe
        // TODO livenessProbe

        // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerproperties.instanceview
        public class InstanceView
        {
            [JsonProperty(PropertyName = "currentState")]
            public ContainerState CurrentState { get; set; }

            [JsonProperty(PropertyName = "events")]
            public List<Event> Events { get; set; }

            [JsonProperty(PropertyName = "previousState")]
            public ContainerState PreviousState { get; set; }

            [JsonProperty(PropertyName = "restartCount")]
            public int RestartCount { get; set; }
        }

    }

}
