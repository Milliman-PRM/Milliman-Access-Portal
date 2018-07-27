using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class UserDetailForProfitCenter
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Dictionary<string, List<string>> AssignedClients { get; set; } = null;

        public static explicit operator UserDetailForProfitCenter(ApplicationUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDetailForProfitCenter
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
            };
        }

        public void QueryRelatedEntities(ApplicationDbContext dbContext, long profitCenterId)
        {
            var roles = dbContext.UserRoleInClient
                .Where(r => r.UserId == Id)
                .Where(r => r.Client.ProfitCenterId == profitCenterId)
                .Select(r => new KeyValuePair<string, string>(r.Client.Name, r.Role.RoleEnum.ToString()));

            var assignedClients = new Dictionary<string, List<string>>();
            foreach (var role in roles)
            {
                if (!assignedClients.Keys.Contains(role.Key))
                {
                    assignedClients[role.Key] = new List<string>();
                }
                assignedClients[role.Key].Add(role.Value);
            }

            AssignedClients = assignedClients;
        }
    }
}
