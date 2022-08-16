using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class CloudError
    {
        [JsonProperty(PropertyName = "error")]
        public CloudErrorBody Error { get; set; }
    }
}
