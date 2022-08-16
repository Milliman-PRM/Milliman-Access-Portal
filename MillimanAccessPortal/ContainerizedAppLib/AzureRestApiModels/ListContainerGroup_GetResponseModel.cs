using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ListContainerGroup_GetResponseModel
    {
        [JsonProperty(PropertyName = "value")]
        public List<ContainerGroup_GetResponseModel> ContainerGroups { get; set; }
    }
}
