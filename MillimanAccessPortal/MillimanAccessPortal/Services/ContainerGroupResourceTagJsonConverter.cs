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
                    ProfitCenterId = new Guid(jsonObject.SelectToken("profitCenterId").Value<string>()),
                    ProfitCenterName = jsonObject.SelectToken("profitCenterName").Value<string>(),
                    ContentItemId = new Guid(jsonObject.SelectToken("contentItemId").Value<string>()),
                    ContentItemName = jsonObject.SelectToken("contentItemName").Value<string>(),
                    SelectionGroupId = jsonObject.SelectToken("selectionGroupId") != null ?
                        new Guid(jsonObject.SelectToken("selectionGroupId").Value<string>())
                        : null,
                    SelectionGroupName = jsonObject.SelectToken("selectionGroupName").Value<string>(),
                    ClientId = new Guid(jsonObject.SelectToken("clientId").Value<string>()),
                    ClientName = jsonObject.SelectToken("clientName").Value<string>(),
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
