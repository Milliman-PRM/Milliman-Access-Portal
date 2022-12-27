using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#storageaccountmicrosoftendpoints
    public class StorageAccountMicrosoftEndpoints
    {
        [JsonProperty(PropertyName = "blob")]
        public string Blob { get; set; }

        [JsonProperty(PropertyName = "dfs")]
        public string Dfs { get; set; }

        [JsonProperty(PropertyName = "file")]
        public string File { get; set; }

        [JsonProperty(PropertyName = "queue")]
        public string Queue { get; set; }

        [JsonProperty(PropertyName = "table")]
        public string Table { get; set; }

        [JsonProperty(PropertyName = "web")]
        public string Web { get; set; }
    }
}
