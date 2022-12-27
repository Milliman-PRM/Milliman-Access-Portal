using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/list?tabs=HTTP#fileshareitems
    public class FileShareItems
    {
        [JsonProperty(PropertyName = "nextLink")]
        public string NextLink { get; set; }

        [JsonProperty(PropertyName = "value")]
        public List<FileShare> Value { get; set; }
    }
}
