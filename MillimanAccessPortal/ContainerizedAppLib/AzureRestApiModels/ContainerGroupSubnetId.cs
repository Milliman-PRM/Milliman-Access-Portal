using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroupSubnetId
    {
        // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containergroupsubnetid

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
