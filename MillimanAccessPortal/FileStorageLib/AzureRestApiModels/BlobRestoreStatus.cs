using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#blobrestorestatus
    public class BlobRestoreStatus
    {
        [JsonProperty(PropertyName = "failureReason")]
        public string FailureReason { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public BlobRestoreParameters BlobRestoreParameters { get; set; }

        [JsonProperty(PropertyName = "restoreId")]
        public string RestoreId { get; set; }

        [JsonProperty(PropertyName = "status")]
        public BlobRestoreProgressStatusEnum BlobRestoreProgressStatus { get; set; }
    }
}
