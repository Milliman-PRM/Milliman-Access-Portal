using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientDetailForUser
    {
        public long Id { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }

        public static explicit operator ClientDetailForUser(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new ClientDetailForUser
            {
                Id = client.Id,
                ClientName = client.Name,
                ClientCode = client.ClientCode,
            };
        }
    }
}
