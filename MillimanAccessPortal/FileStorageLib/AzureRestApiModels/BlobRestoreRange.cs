using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#blobrestorerange
    public class BlobRestoreRange
    {
        [JsonProperty(PropertyName = "startRange")]
        public string StartRange { get; set; }

        [JsonProperty(PropertyName = "endRange")]
        public string EndRange { get; set; }
    }
}
