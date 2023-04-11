using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class VolumeMount
    {
        // https://learn.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update?tabs=HTTP#volumemount

        [JsonProperty(PropertyName = "mountPath")]
        public string MountPath { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "readOnly")]
        public bool ReadOnly { get; set; }
    }
}
