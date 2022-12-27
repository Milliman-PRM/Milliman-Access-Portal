
using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#storageaccountskuconversionstatus
    public class StorageAccountSkuConversionStatus
    {
        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "endTime")]
        public string EndTime { get; set; }

        [JsonProperty(PropertyName = "skuConversionStatus")]
        public SkuConversionStatusEnum SkuConversionStatus { get; set; }

        [JsonProperty(PropertyName = "targetSkuName")]
        public SkuNameEnum SkuName { get; set; }
    }
}
