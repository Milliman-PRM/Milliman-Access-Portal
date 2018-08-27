/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide a common data structure for clients to specify query constraints.
 * DEVELOPER NOTES:
 *      This class is intended to restrict which entities are pulled from the database.
 *      In most use cases, none or one of the properties should be set to a value.
 */


namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class QueryFilter
    {
        public long? UserId { get; set; } = null;
        public long? ClientId { get; set; } = null;
        public long? ProfitCenterId { get; set; } = null;
        public long? RootContentItemId { get; set; } = null;
    }
}
