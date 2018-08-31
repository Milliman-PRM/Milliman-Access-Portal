/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide profit center information for presentation on a profit center card.
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ProfitCenterInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Office { get; set; }
        public int UserCount { get; set; }
        public int ClientCount { get; set; }

        public static explicit operator ProfitCenterInfo(ProfitCenter profitCenter)
        {
            if (profitCenter == null)
            {
                return null;
            }

            return new ProfitCenterInfo
            {
                Id = profitCenter.Id,
                Name = profitCenter.Name,
                Code = profitCenter.ProfitCenterCode,
                Office = profitCenter.MillimanOffice,
            };
        }

        public void QueryRelatedEntityCounts(ApplicationDbContext dbContext)
        {
            // count all users under the profit center
            UserCount = dbContext.UserRoleInProfitCenter
                .Where(role => role.ProfitCenterId == Id)
                .Where(role => role.Role.RoleEnum == RoleEnum.Admin)
                .Count();

            // count all clients under the profit center
            ClientCount = dbContext.Client
                .Where(client => client.ProfitCenterId == Id)
                .Count();
        }
    }
}
