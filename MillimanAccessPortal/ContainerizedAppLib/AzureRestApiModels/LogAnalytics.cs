using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class LogAnalytics
    {
        [JsonProperty(PropertyName = "logType")]
        public LogAnalyticsLogType LogAnalyticsLogType { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public object Metadata { get; set; }

        [JsonProperty(PropertyName = "workspaceId")]
        public string WorkspaceId { get; set; }

        [JsonProperty(PropertyName = "workspaceKey")]
        public string WorkspaceKey { get; set; }

        [JsonProperty(PropertyName = "workspaceResourceId")]
        public string WorkspaceResourceId { get; set; }
    }

    public enum LogAnalyticsLogType
    {
        ContainerInsights,
        ContainerInstanceLogs
    }
}
