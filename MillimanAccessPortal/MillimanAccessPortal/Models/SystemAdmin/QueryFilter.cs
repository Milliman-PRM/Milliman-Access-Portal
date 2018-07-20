/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Linq;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class QueryFilter
    {
        public long? UserId { get; set; } = null;
        public long? ClientId { get; set; } = null;
        public long? ProfitCenterId { get; set; } = null;
        public long? RootContentItemId { get; set; } = null;

        public void Apply(ref IQueryable query)
        {
        }
    }

}
