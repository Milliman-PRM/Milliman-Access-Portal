using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#storageaccount
    public class StorageAccount
    {
        [JsonProperty(PropertyName = "extendedLocation")]
        public ExtendedLocation ExtendedLocation { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "identity")]
        public ResourceIdentity Identity { get; set; }

        [JsonProperty(PropertyName = "kind")]
        public AzureStorageAccountKindEnum StorageAccountKind { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public StorageAccountProperties StorageAccountProperties { get; set; }

        [JsonProperty(PropertyName = "sku")]
        public StorageAccountSku Sku { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public object Tags { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
