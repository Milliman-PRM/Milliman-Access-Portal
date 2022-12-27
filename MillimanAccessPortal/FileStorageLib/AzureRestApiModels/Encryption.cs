using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#encryption
    public class Encryption
    {
        [JsonProperty(PropertyName = "identity")]
        public EncryptionIdentity EncryptionIdentity{ get; set; }

        [JsonProperty(PropertyName = "keySource")]
        public KeySourceEnum KeySource = KeySourceEnum.MicrosoftStorage;

        [JsonProperty(PropertyName = "keyvaultproperties")]
        public KeyVaultProperties KeyVaultProperties{ get; set; }

        [JsonProperty(PropertyName = "requireInfrastructureEncryption")]
        public bool RequireInfrastructureEncryption { get; set; }

        [JsonProperty(PropertyName = "services")]
        public EncryptionServices EncryptionServices { get; set; }
    }
}
