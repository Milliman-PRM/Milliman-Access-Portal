using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerprobe
    public class ContainerProbe
    {
        [JsonProperty(PropertyName = "exec")]
        public ContainerExec ContainerExec { get; set; }

        [JsonProperty(PropertyName = "failureThreshold")]
        public int FailureThreshold { get; set; }

        // TODO httpGet
        // TODO initialDelaySeconds
        // TODO periodSeconds
        // TODO successThreshold
        // TODO timeoutSeconds
    }
}
