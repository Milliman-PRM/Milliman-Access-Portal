using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class Volume
    {
        // https://learn.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update?tabs=HTTP#volume
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "azureFile")]
        public AzureFileVolume AzureFile { get; set; }

        // TODO EmptyDir
        // TODO GitRepo
        // TODO Secret
    }
}
