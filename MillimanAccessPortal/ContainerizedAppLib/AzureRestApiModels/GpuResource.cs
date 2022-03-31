using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#gpuresource
    internal class GpuResource
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "sku")]
        public GpuSkuEnum GpuSku { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum GpuSkuEnum
    {
        K80,
        P100,
        V100
    }
}
