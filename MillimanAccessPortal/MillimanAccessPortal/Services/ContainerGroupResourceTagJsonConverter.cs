using ContainerizedAppLib.AzureRestApiModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ContainerizedAppLib
{
    public class ContainerGroupResourceTagJsonConverter : JsonConverter<ContainerGroupResourceTags>
    {
        public override bool CanRead => base.CanRead;
        public override bool CanWrite => false;

        public override ContainerGroupResourceTags ReadJson(JsonReader reader, Type objectType, ContainerGroupResourceTags existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                JObject jsonObject = JObject.Load(reader);
                ContainerGroupResourceTags tags = new ContainerGroupResourceTags()
                {
                    ProfitCenterId = new Guid((string) jsonObject["profitCenterId"]),
                    ProfitCenterName = (string) jsonObject["profitCenterName"],
                    ContentItemId = new Guid((string) jsonObject["contentItemId"]),
                    ContentItemName = (string) jsonObject["contentItemName"],
                    SelectionGroupId = new Guid((string) jsonObject["selectionGroupId"]),
                    SelectionGroupName = (string) jsonObject["selectionGroupName"],
                    ClientId = new Guid((string) jsonObject["clientId"]),
                    ClientName = (string) jsonObject["clientName"],
                };
                return tags;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Could not deserialize Container Resource Tags.", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, ContainerGroupResourceTags value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
