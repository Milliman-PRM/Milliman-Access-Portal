using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#identity
    public class ResourceIdentity
    {
        [JsonProperty(PropertyName = "principalId")]
        public string PrincipalId { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public IdentityTypeEnum IdentityType { get; set; }

        [JsonProperty(PropertyName = "userAssignedIdentities")]
        public Dictionary<string, UserAssignedIdentity> UserAssignedIdentities { get; set; }
    }
}
