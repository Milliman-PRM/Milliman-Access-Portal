/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class QueryFilter
    {
        public Guid? UserId { get; set; } = null;
        public Guid? ClientId { get; set; } = null;
        public Guid? ProfitCenterId { get; set; } = null;
        public Guid? RootContentItemId { get; set; } = null;
    }
}
