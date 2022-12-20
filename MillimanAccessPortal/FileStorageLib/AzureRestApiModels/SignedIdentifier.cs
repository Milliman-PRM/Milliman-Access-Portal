using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#signedidentifier
    public class SignedIdentifier
    {
        [JsonProperty(PropertyName = "accessPolicy")]
        public AccessPolicy AccessPolicy { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
