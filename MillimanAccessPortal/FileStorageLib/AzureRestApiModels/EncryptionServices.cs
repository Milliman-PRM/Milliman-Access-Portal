using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#encryptionservices
    public class EncryptionServices
    {
        [JsonProperty(PropertyName = "blob")]
        public EncryptionService Blob { get; set; }

        [JsonProperty(PropertyName = "file")]
        public EncryptionService File { get; set; }

        [JsonProperty(PropertyName = "Queue ")]
        public EncryptionService Queue { get; set; }


        [JsonProperty(PropertyName = "table")]
        public EncryptionService Table { get; set; }
    }
}
