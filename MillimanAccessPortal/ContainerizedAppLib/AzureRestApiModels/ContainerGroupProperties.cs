using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroupProperties
    {
        // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/get#containergroup
        [JsonProperty(PropertyName = "containers")]
        public List<Container> Containers { get; set; } // Required.

        [JsonProperty(PropertyName = "imageRegistryCredentials")]
        public List<ImageRegistryCredential> ImageRegistryCredentials { get; set; }

        [JsonProperty(PropertyName = "ipAddress")]
        public IpAddress IpAddress { get; set; }

        [JsonProperty(PropertyName = "diagnostics")]
        public ContainerGroupDiagnostics Diagnostics { get; set; }

        [JsonProperty(PropertyName = "dnsConfig")]
        public DnsConfiguration dnsConfig { get; set; }

        // TODO encryptionProperties
        // TODO imageRegistryCredentials
        // TODO initContainers

        [JsonProperty(PropertyName = "instanceView")]
        public InstanceView InstanceView { get; set; }

        // TODO ipAddress

        [JsonProperty(PropertyName = "osType")]
        public OsTypeEnum OsType { get; set; } // Required.

        [JsonProperty(PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        // TODO restartPolicy
        // TODO sku
        // TODO subnetIds
        // TODO volumes
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OsTypeEnum
    {
        Linux,
        Windows
    }

}
