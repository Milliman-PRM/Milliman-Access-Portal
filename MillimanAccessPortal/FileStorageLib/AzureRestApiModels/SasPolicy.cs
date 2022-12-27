using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#saspolicy
    public class SasPolicy
    {
        [JsonProperty(PropertyName = "expirationAction")]
        public SasExpirationAction ExpirationAction { get; set; } = SasExpirationAction.Log;

        [JsonProperty(PropertyName = "sasExpirationPeriod")]
        public string SasExpirationPeriod { get; set; }
    }
}
