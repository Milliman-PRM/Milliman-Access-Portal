/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Contains mapping of fields with csv files accessed using package CsvHelper
 * DEVELOPER NOTES: https://joshclose.github.io/CsvHelper/getting-started
 */

using AuditLogLib.Models;
using CsvHelper.Configuration;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.ClientAccessReview;
using Newtonsoft.Json;

namespace MillimanAccessPortal.Models.ClientAccessReview
{
    public class ContentAccessRowItem
    {
        public string ContentName { get; set; }
        public string SelectionGroupName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public bool Suspended { get; set; }
    }

    public class ContentAccessCsvMap : ClassMap<ContentAccessRowItem>
    {
        public ContentAccessCsvMap()
        {
            Map(m => m.ContentName).Index(0).Name("Content Name");
            Map(m => m.SelectionGroupName).Index(1).Name("Selection Group");
            Map(m => m.UserName).Index(2).Name("User");
            Map(m => m.UserEmail).Index(3).Name("Email");
            Map(m => m.Suspended).Index(4).Name("Selection Group Suspended").ConvertUsing(m => m.Suspended ? "Yes" : "No");
        }
    }
}
