using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#storageaccount See properties
    public class StorageAccountProperties
    {
        [JsonProperty(PropertyName = "accessTier")]
        public AccessTierEnum AccessTier { get; set; }

        [JsonProperty(PropertyName = "allowBlobPublicAccess")]
        public bool AllowBlobPublicAccess { get; set; }

        [JsonProperty(PropertyName = "allowCrossTenantReplication")]
        public bool AllowCrossTenantReplication { get; set; }

        [JsonProperty(PropertyName = "allowSharedKeyAccess")]
        public bool AllowSharedKeyAccess { get; set; }

        [JsonProperty(PropertyName = "allowedCopyScope")]
        public AllowedCopyScopeEnum AllowedCopyScope { get; set; }

        [JsonProperty(PropertyName = "azureFilesIdentityBasedAuthentication")]
        public AzureFilesIdentityBasedAuthentication AzureFilesIdentityBasedAuthentication { get; set; }

        [JsonProperty(PropertyName = "blobRestoreStatus")]
        public BlobRestoreStatus BlobRestoreStatus { get; set; }

        [JsonProperty(PropertyName = "creationTime")]
        public string CreationTime { get; set; }

        [JsonProperty(PropertyName = "customDomain")]
        public CustomDomain CustomDomain { get; set; }

        [JsonProperty(PropertyName = "defaultToOAuthAuthentication")]
        public bool DefaultToOAuthAuthentication { get; set; }

        [JsonProperty(PropertyName = "dnsEndpointType")]
        public DnsEndpointTypeEnum DnsEndpointType { get; set; }

        [JsonProperty(PropertyName = "encryption")]
        public Encryption Encryption { get; set; }

        [JsonProperty(PropertyName = "failoverInProgress")]
        public bool FailoverInProgress { get; set; }

        [JsonProperty(PropertyName = "geoReplicationStats")]
        public GeoReplicationStats GeoReplicationStats { get; set; }

        [JsonProperty(PropertyName = "immutableStorageWithVersioning")]
        public ImmutableStorageAccount ImmutableStorageWithVersioning { get; set; }

        [JsonProperty(PropertyName = "isHnsEnabled")]
        public bool IsHnsEnabled { get; set; }

        [JsonProperty(PropertyName = "isLocalUserEnabled")]
        public bool IsLocalUserEnabled { get; set; }

        [JsonProperty(PropertyName = "isNfsV3Enabled")]
        public bool IsNfsV3Enabled { get; set; }

        [JsonProperty(PropertyName = "isSftpEnabled")]
        public bool IsSftpEnabled { get; set; }

        [JsonProperty(PropertyName = "keyCreationTime")]
        public KeyCreationTime KeyCreationTime { get; set; }

        [JsonProperty(PropertyName = "keyPolicy")]
        public KeyPolicy KeyPolicy { get; set; }

        [JsonProperty(PropertyName = "largeFileShareState")]
        public LargeFileShareStateEnum LargeFileShareState { get; set; }

        [JsonProperty(PropertyName = "lastGeoFailoverTime")]
        public string LastGeoFailoverTime { get; set; }

        [JsonProperty(PropertyName = "minimumTlsVersion")]
        public MinimumTlsVersionEnum MinimumTlsVersion { get; set; }

        [JsonProperty(PropertyName = "networkAcls")]
        public NetworkRuleSet NetworkRuleSet { get; set; }

        [JsonProperty(PropertyName = "primaryEndpoints")]
        public Endpoints PrimaryEndpoints { get; set; }

        [JsonProperty(PropertyName = "primaryLocation")]
        public string PrimaryLocation { get; set; }

        [JsonProperty(PropertyName = "privateEndpointConnections")]
        public List<PrivateEndpointConnection> PrivateEndpointConnections { get; set; }

        [JsonProperty(PropertyName = "provisioningState")]
        public ProvisioningStateEnum ProvisioningState { get; set; }

        [JsonProperty(PropertyName = "publicNetworkAccess")]
        public PublicNetworkAccessEnum PublicNetworkAccess { get; set; }

        [JsonProperty(PropertyName = "routingPreference")]
        public RoutingPreference RoutingPreference { get; set; }

        [JsonProperty(PropertyName = "sasPolicy")]
        public SasPolicy SasPolicy { get; set; }

        [JsonProperty(PropertyName = "secondaryEndpoints")]
        public Endpoints SecondaryEndpoints { get; set; }

        [JsonProperty(PropertyName = "secondaryLocation")]
        public string SecondaryLocation { get; set; }

        [JsonProperty(PropertyName = "statusOfPrimary")]
        public AccountStatusEnum StatusOfPrimary { get; set; }

        [JsonProperty(PropertyName = "statusOfSecondary")]
        public AccountStatusEnum StatusOfSecondary { get; set; }

        [JsonProperty(PropertyName = "storageAccountSkuConversionStatus")]
        public StorageAccountSkuConversionStatus StorageAccountSkuConversionStatus { get; set; }

        [JsonProperty(PropertyName = "supportsHttpsTrafficOnly")]
        public bool SupportsHttpsTrafficOnly { get; set; }
    }
}
