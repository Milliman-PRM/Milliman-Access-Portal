using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#keycreationtime
    public class KeyCreationTime
    {
        [JsonProperty(PropertyName = "key1")]
        public string Key1 { get; set; }

        [JsonProperty(PropertyName = "key2")]
        public string Key2 { get; set; }
    }
}
