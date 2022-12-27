using FileStorageLib.AzureRestApiModels;
using Newtonsoft.Json;

namespace FileStorageLib.RequestModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#request-body
    public class CreateStorageAccountRequestModel
    {
        [JsonProperty(PropertyName = "kind")]
        public AzureStorageAccountKindEnum StorageAccountKind { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "sku")]
        public StorageAccountSku Sku { get; set; }

        [JsonProperty(PropertyName = "extendedLocation")]
        public ExtendedLocation ExtendedLocation { get; set; }

        [JsonProperty(PropertyName = "identity")]
        public ResourceIdentity Identity { get; set; }

        // TODO: properties

        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }
    }
}
