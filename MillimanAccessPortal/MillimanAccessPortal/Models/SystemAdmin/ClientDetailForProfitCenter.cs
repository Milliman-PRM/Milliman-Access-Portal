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
        public NestedList AuthorizedUsers { get; set; } = null;

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

            var authorizedUsers = new NestedList();
            foreach (var role in roles)
            {
                if (!authorizedUsers.Sections.Any(s => s.Name == role.Key))
                {
                    authorizedUsers.Sections.Add(new NestedListSection
                    {
                        Name = role.Key,
                    });
                }
                authorizedUsers.Sections.Single(s => s.Name == role.Key).Values.Add(role.Value);
            }

            AuthorizedUsers = authorizedUsers;
        }
    }
}
