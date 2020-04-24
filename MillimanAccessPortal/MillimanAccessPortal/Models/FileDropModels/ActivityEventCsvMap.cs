/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Contains mapping of fields with csv files accessed using package CsvHelper
 * DEVELOPER NOTES: https://joshclose.github.io/CsvHelper/getting-started
 */

using AuditLogLib.Models;
using CsvHelper.Configuration;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class ActivityEventCsvMap : ClassMap<ActivityEventModel>
    {
        public ActivityEventCsvMap()
        {
            Map(m => m.TimeStampUtc).Index(0).Name("TimeStamp(Utc)");
            Map(m => m.EventType).Index(1).Name("EventType");
            Map(m => m.UserName).Index(2).Name("UserName");
            Map(m => m.FullName).Index(3).Name("FullName");
            Map(m => m.EventData).Index(4).Name("EventData");
        }
    }
}
