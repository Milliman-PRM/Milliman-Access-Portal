using MapDbContextLib.Context;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientDetailForProfitCenter
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public Dictionary<string, List<string>> AuthorizedUsers { get; set; } = null;

        public static explicit operator ClientDetailForProfitCenter(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new ClientDetailForProfitCenter
            {
                Id = client.Id,
                Name = client.Name,
                Code = client.ClientCode,
                ContactName = client.ContactName,
                ContactEmail = client.ContactEmail,
                ContactPhone = client.ContactPhone,
            };
        }
    }
}
