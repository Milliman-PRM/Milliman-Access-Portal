using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#blobrestoreparameters
    public class BlobRestoreParameters
    {
        [JsonProperty(PropertyName = "blobRanges")]
        public List<BlobRestoreRange> BlobRanges { get; set; }

        [JsonProperty(PropertyName = "timeToRestore")]
        public string TimeToRestore { get; set; }
    }
}
