using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#resourcerequirements
    internal class ResourceRequirements
    {
        [JsonProperty(PropertyName = "limits")]
        public ResourceDescriptor ResourceLimits { get; set; }

        [JsonProperty(PropertyName = "requests")]
        public ResourceDescriptor ResourceRequests { get; set; }
    }
}
