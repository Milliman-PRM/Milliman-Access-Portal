using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerstate
    internal class ContainerState
    {
        [JsonProperty(PropertyName = "detailStatus")]
        public string DetailStatus { get; set; }

        [JsonProperty(PropertyName = "exitCode")]
        public int ExitCode { get; set; }

        [JsonProperty(PropertyName = "finishTime")]
        public string FinishTime { get; set; }

        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }
}
