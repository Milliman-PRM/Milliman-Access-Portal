using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerport
    internal class ContainerPort
    {
        [JsonProperty(PropertyName = "port")]
        public ushort Port { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public ProtocolEnum Protocol { get; set; } = ProtocolEnum.TCP;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum ProtocolEnum
    {
        TCP,
        UDP
    }
}
