using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#request-body
    internal class AzureContainerGroupRequestModel
    {
        [JsonProperty(PropertyName = "properties")]
        public ContainerGroupProperties Properties { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        
        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }
        
        [JsonProperty(PropertyName = "zones")]
        public List<string> Zones { get; set; }

        // TODO identity
    }

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
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#container
    internal class Container
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ContainerProperties Properties { get; set; }
    }

    internal class ContainerProperties
    {
        [JsonProperty(PropertyName = "command")]
        public List<string> Commands { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "instanceView")]
        public InstanceView InstanceView { get; set; }
        
        [JsonProperty(PropertyName = "ports")]
        public List<ContainerPort> Ports { get; set; }
        
        [JsonProperty(PropertyName = "resources")]
        public ResourceRequirements Resources { get; set; }

        // TODO environmentVariables
        // TODO volumeMounts
        // TODO readinessProbe
        // TODO livenessProbe

    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerport
    internal class ContainerPort
    {
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public ProtocolEnum Protocol { get; set; } = ProtocolEnum.TCP;
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#imageregistrycredential
    internal class ImageRegistryCredential
    {   
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }

        /*
         * For managed Identity
         * 
         * [JsonProperty(PropertyName = "identity")]
         * public string Identity { get; set; }

         * [JsonProperty(PropertyName = "identityUrl")]
         * public string IdentityUrl { get; set; }
        */
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#resourcerequirements
    internal class ResourceRequirements
    {
        [JsonProperty(PropertyName = "limits")]
        public ResourceData ResourceLimits { get; set; }

        [JsonProperty(PropertyName = "requests")]
        public ResourceData ResourceRequests { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#resourcelimits
    internal class ResourceData
    {
        [JsonProperty(PropertyName = "cpu")]
        public int CpuLimit { get; set; }

        [JsonProperty(PropertyName = "memoryInGB")]
        public double MemoryInGB { get; set; }

        // Including this defaults to SKU K80, which will cost quite a bit in performance.
        // [JsonProperty(PropertyName = "gpu")]
        // public GpuResource GpuResource { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#gpuresource
    internal class GpuResource
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "sku")]
        public GpuSkuEnum GpuSku { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerproperties.instanceview
    internal class InstanceView
    {
        [JsonProperty(PropertyName = "currentState")]
        public ContainerState CurrentState { get; set; }

        [JsonProperty(PropertyName = "previousState")]
        public ContainerState PreviousState { get; set; }

        [JsonProperty(PropertyName = "restartCount")]
        public int RestartCount { get; set; }

        [JsonProperty(PropertyName = "events")]
        public List<ContainerInstanceEvent> Events { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerstate
    internal class ContainerState
    {
        [JsonProperty(PropertyName = "detailStatus")]
        public string DetailStatus { get; set; }

        [JsonProperty(PropertyName = "exitCode")]
        public int ExitCode { get; set; }
        
        [JsonProperty(PropertyName = "finishTime")]
        public string FinishTime { get; set; }

        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#event
    internal class ContainerInstanceEvent
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "firstTimestamp")]
        public string FirstTimestamp { get; set; }

        [JsonProperty(PropertyName = "lastTimestamp")]
        public string LastTimestamp { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerprobe
    internal class ContainerProbe
    {
        [JsonProperty(PropertyName = "exec")]
        public ContainerExec ContainerExec { get; set; }

        [JsonProperty(PropertyName = "failureThreshold")]
        public int FailureThreshold { get; set; }

        // TODO httpGet
        // TODO initialDelaySeconds
        // TODO periodSeconds
        // TODO successThreshold
        // TODO timeoutSeconds
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerexec
    internal class ContainerExec
    {
        [JsonProperty(PropertyName = "command")]
        public List<string> Command { get; set; }
    }

    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#ipaddress
    internal class IpAddress
    {
        [JsonProperty(PropertyName = "dnsNameLabel")]
        public string DnsNameLabel { get; set; }
        
        [JsonProperty(PropertyName = "fqdn")]
        public string Fqdn { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string Ip { get; set; }

        [JsonProperty(PropertyName = "ports")]
        public List<ContainerPort> Ports { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = "Public"; // Can also be set to "Private"
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum OsTypeEnum
    {
        Linux,
        Windows
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum ProtocolEnum
    {
        TCP,
        UDP
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum GpuSkuEnum
    {
        K80,
        P100,
        V100
    }
}
