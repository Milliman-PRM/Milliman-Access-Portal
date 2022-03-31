using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib.AzureRestApiModels
{
    internal class ContainerGroupProperties
    {
        [JsonProperty(PropertyName = "osType")]
        public OsTypeEnum OsType { get; set; } // Required.

        [JsonProperty(PropertyName = "containers")]
        public List<Container> Containers { get; set; } // Required.

        [JsonProperty(PropertyName = "imageRegistryCredentials")]
        public List<ImageRegistryCredential> ImageRegistryCredentials { get; set; }

        [JsonProperty(PropertyName = "ipAddress")]
        public IpAddress IpAdress { get; set; }

        // TODO diagnostics
        // TODO dnsConfig
        // TODO encryptionProperties
        // TODO initContainers
        // TODO restartPolicy
        // TODO sku
        // TODO subnetIds
        // TODO volumes

        // TODO ProvisioningState
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum OsTypeEnum
    {
        Linux,
        Windows
    }

}
