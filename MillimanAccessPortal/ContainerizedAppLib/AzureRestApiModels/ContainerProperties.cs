using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerProperties
    {
        [JsonProperty(PropertyName = "command")]
        public List<string> Commands { get; set; }

        [JsonProperty(PropertyName = "environmentVariables")]
        public List<EnvironmentVariable> EnvironmentVariables { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "instanceView")]
        public InstanceView Instance_View { get; set; }

        [JsonProperty(PropertyName = "ports")]
        public List<ContainerPort> Ports { get; set; }

        [JsonProperty(PropertyName = "resources")]
        public ResourceRequirements Resources { get; set; }

        public class EnvironmentVariable
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "secureValue")]
            public string SecureValue { get; set; }

            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }

        [JsonProperty(PropertyName = "volumeMounts")]
        public List<VolumeMount> VolumeMounts { get; set; } = null;

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
