using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#keypolicy
    public class KeyPolicy
    {
        [JsonProperty(PropertyName = "keyExpirationPeriodInDays")]
        public int KeyExpirationPeriodInDays { get; set; }
    }
}
