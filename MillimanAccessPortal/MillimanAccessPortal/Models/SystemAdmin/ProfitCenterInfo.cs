/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class ProfitCenterInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }

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
            };
        }
    }
}
