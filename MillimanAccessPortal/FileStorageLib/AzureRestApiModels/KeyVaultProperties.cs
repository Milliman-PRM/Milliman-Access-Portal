using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#keyvaultproperties
    public class KeyVaultProperties
    {
        [JsonProperty(PropertyName = "currentVersionedKeyExpirationTimestamp")]
        public string CurrentVersionedKeyExpirationTimestamp { get; set; }

        [JsonProperty(PropertyName = "currentVersionedKeyIdentifier")]
        public string CurrentVersionedKeyIdentifier { get; set; }

        [JsonProperty(PropertyName = "keyname")]
        public string KeyName { get; set; }

        [JsonProperty(PropertyName = "keyvaulturi")]
        public string KeyVaultUri { get; set; }

        [JsonProperty(PropertyName = "keyversion")]
        public string KeyVersion { get; set; }

        [JsonProperty(PropertyName = "lastKeyRotationTimestamp")]
        public string LastKeyRotationTimestamp { get; set; }
    }
}
