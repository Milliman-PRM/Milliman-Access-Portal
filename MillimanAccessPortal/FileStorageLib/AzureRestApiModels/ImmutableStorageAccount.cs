using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#immutablestorageaccount
    public class ImmutableStorageAccount
    {
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }
        
        [JsonProperty(PropertyName = "immutabilityPolicy")]
        public AccountImmutabilityPolicyProperties ImmutabilityPolicy { get; set; }
    }
}
