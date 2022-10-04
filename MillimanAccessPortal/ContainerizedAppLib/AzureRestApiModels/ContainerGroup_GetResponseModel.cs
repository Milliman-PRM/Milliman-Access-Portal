using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroup_GetResponseModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        // TODO
        // [JsonProperty(PropertyName = "identity")]
        // public ContainerGroupIdentity Identity { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "properties")]
        public ContainerGroupProperties Properties { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public Dictionary<string,string> Tags { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "zones")]
        public List<string> Zones { get; set; }

        [JsonIgnore]
        public Uri Uri
        {
            get
            {
                try
                {
                    UriBuilder uriBuilder = new UriBuilder
                    {
                        // TODO We currently don't support a redirect to another port (e.g. https redirect). If we decide to support SSL in containerized apps 
                        // we need logic to choose the correct scheme here. This may require a new type specific publishing property (e.g. SSL check box?)
                        Scheme = "http",  
                        Host = Properties.IpAddress.Ip,
                        Port = Properties.IpAddress.Ports.SingleOrDefault().Port,
                    };
                    return uriBuilder.Uri;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
