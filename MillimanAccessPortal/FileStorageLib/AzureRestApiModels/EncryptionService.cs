using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#encryptionidentity
    public class EncryptionService
    {
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; } = false; // TODO: Determine default value

        [JsonProperty(PropertyName = "keyType")]
        public KeyTypeEnum KeyType { get; set; }

        [JsonProperty(PropertyName = "lastEnabledTime")]
        public string LastEnabledTime { get; set; }
    }
}
