using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClient
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; } = null;
        public string Name { get; set; }
        public string Code { get; set; }

        public static explicit operator BasicClient(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new BasicClient
            {
                Id = client.Id,
                ParentId = client.ParentClientId,
                Name = client.Name,
                Code = client.ClientCode,
            };
        }
    }
}
