
using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#fileshare
    public class FileShare
    {
        [JsonProperty(PropertyName = "etag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public FileShareProperties Properties { get; set; }
    }
}
