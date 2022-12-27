using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#encryptionidentity
    public class EncryptionIdentity
    {
        [JsonProperty(PropertyName = "federatedIdentityClientId")]
        public string FederatedIdentityClientId { get; set; }

        [JsonProperty(PropertyName = "userAssignedIdentity")]
        public string UserAssignedIdentity { get; set; }
    }
}
