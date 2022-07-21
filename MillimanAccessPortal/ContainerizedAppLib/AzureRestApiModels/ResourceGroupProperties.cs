using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/resources/resource-groups/get#resourcegroup

    public class ResourceGroupProperties
    {
        [JsonProperty(PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }
    }
}
