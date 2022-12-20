using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#accesspolicy
    public class AccessPolicy
    {
        [JsonProperty(PropertyName = "expiryTime")]
        public string ExpiryTime { get; set; }

        [JsonProperty(PropertyName = "permission")]
        public string Permission { get; set; }

        [JsonProperty(PropertyName = "startTime")]
        public string StartTime { get; set; }
    }
}
