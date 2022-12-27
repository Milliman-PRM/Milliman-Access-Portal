using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#endpoints
    public class Endpoints
    {
        [JsonProperty(PropertyName = "blob")]
        public string Blob { get; set; }

        [JsonProperty(PropertyName = "dfs")]
        public string Dfs { get; set; }

        [JsonProperty(PropertyName = "file")]
        public string File { get; set; }

        [JsonProperty(PropertyName = "internetEndpoints")]
        public StorageAccountInternetEndpoints InternetEndpoints { get; set; }

        [JsonProperty(PropertyName = "microsoftEndpoints")]
        public StorageAccountMicrosoftEndpoints MicrosoftEndpoints { get; set; }

        [JsonProperty(PropertyName = "queue")]
        public string Queue { get; set; }

        [JsonProperty(PropertyName = "table")]
        public string Table { get; set; }

        [JsonProperty(PropertyName = "web")]
        public string Web { get; set; }
    }
}
