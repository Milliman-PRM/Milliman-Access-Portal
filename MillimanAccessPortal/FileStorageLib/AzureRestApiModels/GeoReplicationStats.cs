using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#georeplicationstats
    public class GeoReplicationStats
    {
        [JsonProperty(PropertyName = "canFailover")]
        public bool CanFailover { get; set; }

        [JsonProperty(PropertyName = "lastSyncTime")]
        public string LastSyncTime { get; set; }

        [JsonProperty(PropertyName = "status")]
        public GeoReplicationStatusEnum Status { get; set; }
    }
}
