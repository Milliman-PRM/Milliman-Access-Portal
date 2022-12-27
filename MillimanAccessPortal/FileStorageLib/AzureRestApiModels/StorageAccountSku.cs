using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#sku
    public class StorageAccountSku
    {
        [JsonProperty(PropertyName = "name")]
        public SkuNameEnum SkuName { get; set; }

        [JsonProperty(PropertyName = "tier")]
        public SkuTierEnum SkuTier { get; set; }
    }
}
