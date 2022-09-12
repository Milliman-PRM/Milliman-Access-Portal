using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroupDiagnostics
    {
        [JsonProperty(PropertyName = "logAnalytics")]
        public LogAnalytics LogAnalytics { get; set; }
    }
}
