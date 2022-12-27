using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#privateendpoint
    public class PrivateEndpoint
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
