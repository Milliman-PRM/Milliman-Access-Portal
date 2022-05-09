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

        [JsonProperty(PropertyName = "risk_level")]
        public string RiskLevel { get; set; } = "4";

        [JsonProperty(PropertyName = "managed_by")]
        public string ManagedBy { get; set; } = "PRM Analytics";

        // Consider the environment property of the IHost service.
        [JsonProperty(PropertyName = "environment")]
        public string Environment => System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        [JsonProperty(PropertyName = "asset_owner")]
        public string AssetOwner => ProfitCenterName;

        [JsonProperty(PropertyName = "content_status")]
        public string ContentStatus { get; set; }
    }
}
