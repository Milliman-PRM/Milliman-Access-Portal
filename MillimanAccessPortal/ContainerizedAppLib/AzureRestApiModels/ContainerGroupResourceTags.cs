using Newtonsoft.Json;
using System;

namespace ContainerizedAppLib.AzureRestApiModels
{
    public class ContainerGroupResourceTags
    {
        [JsonProperty(PropertyName = "profitCenterId")]
        public Guid ProfitCenterId { get; set; }

        [JsonProperty(PropertyName = "profitCenterName")]
        public string ProfitCenterName { get; set; }

        [JsonProperty(PropertyName = "clientId")]
        public Guid ClientId { get; set; }

        [JsonProperty(PropertyName = "clientName")]
        public string ClientName { get; set; }

        [JsonProperty(PropertyName = "contentItemId")]
        public Guid ContentItemId { get; set; }

        [JsonProperty(PropertyName = "contentItemName")]
        public string ContentItemName { get; set; }

        [JsonProperty(PropertyName = "selectionGroupId")]
        public Guid? SelectionGroupId { get; set; }

        [JsonProperty(PropertyName = "selectionGroupName")]
        public string SelectionGroupName { get; set; }
    }
}
