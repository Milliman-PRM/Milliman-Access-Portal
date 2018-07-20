/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public static explicit operator ClientInfo(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return new ClientInfo
            {
                Id = client.Id,
                Name = client.Name,
            };
        }
    }
}
