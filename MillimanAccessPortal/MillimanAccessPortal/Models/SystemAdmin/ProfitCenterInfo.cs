/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ProfitCenterInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Office { get; set; }
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
                Office = profitCenter.MillimanOffice,
            };
        }

        public void QueryRelatedEntityCounts(ApplicationDbContext dbContext)
        {
            // count all clients under the profit center
            ClientCount = dbContext.Client
                .Where(client => client.ProfitCenterId == Id)
                .Count();
        }
    }
}
