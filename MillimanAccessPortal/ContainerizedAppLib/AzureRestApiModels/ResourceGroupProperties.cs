using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ResourceGroupProperties
    {
        [JsonProperty(PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }
    }
}
