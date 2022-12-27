using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#azurefilesidentitybasedauthentication
    public class AzureFilesIdentityBasedAuthentication
    {
        [JsonProperty(PropertyName = "activeDirectoryProperties")]
        public ActiveDirectoryProperties ActiveDirectoryProperties { get; set; }

        [JsonProperty(PropertyName = "defaultSharePermission")]
        public DefaultSharePermissionEnum DefaultSharePermission { get; set; }

        [JsonProperty(PropertyName = "directoryServiceOptions")]
        public DirectoryServiceOptionsEnum DirectoryServiceOptions { get; set; }
    }
}
