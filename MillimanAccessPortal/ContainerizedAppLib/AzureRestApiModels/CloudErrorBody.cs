using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class CloudErrorBody
    {
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "details")]
        public string Details { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "target")]
        public string Target { get; set; }
    }
}
