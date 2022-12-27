using Newtonsoft.Json;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#activedirectoryproperties
    public class ActiveDirectoryProperties
    {
        [JsonProperty(PropertyName = "accountType")]
        public ActiveDirectoryAccountTypeEnum AccountType { get; set; }

        [JsonProperty(PropertyName = "azureStorageSid")]
        public string AzureStorageSid { get; set; }

        [JsonProperty(PropertyName = "domainGuid")]
        public string DomainGuid { get; set; }

        [JsonProperty(PropertyName = "domainName")]
        public string DomainName { get; set; }

        [JsonProperty(PropertyName = "domainSid")]
        public string DomainSid { get; set; }

        [JsonProperty(PropertyName = "forestName")]
        public string ForestName { get; set; }

        [JsonProperty(PropertyName = "netBiosDomainName")]
        public string NetBioDomainName { get; set; }

        [JsonProperty(PropertyName = "samAccountName")]
        public string SAMAccountName { get; set; }
    }
}
