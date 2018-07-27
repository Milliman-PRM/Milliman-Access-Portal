using MapDbContextLib.Context;
using System.Collections.Generic;
using System.Linq;

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

        public void QueryRelatedEntities(ApplicationDbContext dbContext, long profitCenterId)
        {
            var roles = dbContext.UserRoleInClient
                .Where(r => r.ClientId == Id)
                .Where(r => r.Client.ProfitCenterId == profitCenterId)
                .Select(r => new KeyValuePair<string, string>(r.Role.RoleEnum.ToString(), $"{r.User.FirstName} {r.User.LastName}"));

            var authorizedUsers = new Dictionary<string, List<string>>();
            foreach (var role in roles)
            {
                if (!authorizedUsers.Keys.Contains(role.Key))
                {
                    authorizedUsers[role.Key] = new List<string>();
                }
                authorizedUsers[role.Key].Add(role.Value);
            }

            AuthorizedUsers = authorizedUsers;
        }
    }
}
