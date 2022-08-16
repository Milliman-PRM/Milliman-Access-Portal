using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class Logs
    {
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }
}
