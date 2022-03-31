using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#resourcelimits
    internal class ResourceDescriptor
    {
        [JsonProperty(PropertyName = "cpu")]
        public int CpuLimit { get; set; }

        [JsonProperty(PropertyName = "memoryInGB")]
        public double MemoryInGB { get; set; }

        // Including this defaults to SKU K80, which will cost quite a bit in performance.
        // [JsonProperty(PropertyName = "gpu")]
        // public GpuResource GpuResource { get; set; }
    }
}
