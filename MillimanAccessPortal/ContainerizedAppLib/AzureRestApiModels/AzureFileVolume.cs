using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class AzureFileVolume
    {
        // https://learn.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update?tabs=HTTP#azurefilevolume

        [JsonProperty(PropertyName = "readOnly")]
        public bool ReadOnly { get; set; } //The flag indicating whether the Azure File shared mounted as a volume is read-only.

        [JsonProperty(PropertyName = "shareName")]
        public string ShareName { get; set; } // The name of the Azure File share to be mounted as a volume.

        [JsonProperty(PropertyName = "storageAccountKey")]
        public string StorageAccountKey { get; set; } // The storage account access key used to access the Azure File share.

        [JsonProperty(PropertyName = "storageAccountName")]
        public string StorageAccountName { get; set; } // The name of the storage account that contains the Azure File share.    }
    }
}
