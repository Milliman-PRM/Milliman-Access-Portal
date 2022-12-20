using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#fileshare (See Properties)
    public class FileShareProperties
    {
        [JsonProperty(PropertyName = "accessTier")]
        public ShareAccessTierEnum AccessTier { get; set; }

        [JsonProperty(PropertyName = "accessTierChangeTime")]
        public string AccessTierChangeTime { get; set; }

        [JsonProperty(PropertyName = "accessTierStatus")]
        public string AccessTierStatus { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        public bool Deleted { get; set; }

        [JsonProperty(PropertyName = "deletedTime")]
        public string DeletedTime { get; set; }

        [JsonProperty(PropertyName = "enabledProtocols")]
        public EnabledProtocolsEnum EnabledProtocols { get; set; }

        [JsonProperty(PropertyName = "lastModifiedTime")]
        public string LastModifiedTime { get; set; }

        [JsonProperty(PropertyName = "leaseDuration")]
        public LeaseDurationEnum LeaseDuration { get; set; }

        [JsonProperty(PropertyName = "leaseState")]
        public LeaseStateEnum LeaseState { get; set; }

        [JsonProperty(PropertyName = "leaseStatus")]
        public LeaseStatusEnum LeaseStatus { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public object Metadata { get; set; }

        [JsonProperty(PropertyName = "remainingRetentionDays")]
        public int RemainingRetentionDays { get; set; }

        [JsonProperty(PropertyName = "rootSquash")]
        public RootSquashTypeEnum RootSquashType { get; set; }

        [JsonProperty(PropertyName = "shareQuota")]
        public int ShareQuota { get; set; }

        [JsonProperty(PropertyName = "shareUsageBytes")]
        public int ShareUsageBytes { get; set; }

        [JsonProperty(PropertyName = "signedIdentifiers")]
        public List<SignedIdentifier> SignedIdentifiers { get; set; }

        [JsonProperty(PropertyName = "snapshotTime")]
        public string SnapshotTime { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
