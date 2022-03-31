using Newtonsoft.Json;
using System.Collections.Generic;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#containerexec
    internal class ContainerExec
    {
        [JsonProperty(PropertyName = "command")]
        public List<string> Command { get; set; }
    }
}
