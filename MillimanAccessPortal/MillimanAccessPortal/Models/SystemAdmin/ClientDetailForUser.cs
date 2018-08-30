/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide client information for display in the system admin detail panel
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ClientDetailForUser
    {
        public Guid Id { get; set; }
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
