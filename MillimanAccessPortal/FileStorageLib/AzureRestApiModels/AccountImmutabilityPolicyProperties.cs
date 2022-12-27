using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#accountimmutabilitypolicyproperties
    public class AccountImmutabilityPolicyProperties
    {
        [JsonProperty(PropertyName = "allowProtectedAppendWrites")]
        public bool AllowProtectedAppendWrites { get; set; }

        [JsonProperty(PropertyName = "immutabilityPeriodSinceCreationInDays")]
        public int ImmutabilityPeriodSinceCreationInDays { get; set; }

        [JsonProperty(PropertyName = "state")]
        public AccountImmutabilityPolicyState AccountImmutabilityPolicyState { get; set; }

    }
}
