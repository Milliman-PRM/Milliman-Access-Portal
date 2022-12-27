using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#storageaccountinternetendpoints
    public class StorageAccountInternetEndpoints
    {
        [JsonProperty(PropertyName = "blob")]
        public string Blob { get; set; }

        [JsonProperty(PropertyName = "dfs")]
        public string Dfs { get; set; }

        [JsonProperty(PropertyName = "file")]
        public string File { get; set; }

        [JsonProperty(PropertyName = "web")]
        public string Web { get; set; }
    }
}
