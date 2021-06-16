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
    public class UserRolesRowItem
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string LastLoginDate { get; set; }
        public bool IsClientAdmin { get; set; }
        public bool IsContentPublisher { get; set; }
        public bool IsContentAccessAdmin { get; set; }
        public bool IsContentUser { get; set; }
        public bool IsFileDropAdmin { get; set; }
        public bool IsFileDropUser { get; set; }
    }
    public class UserRolesCsvMap : ClassMap<UserRolesRowItem>
    {
        public UserRolesCsvMap()
        {
            Map(m => m.UserName).Index(0).Name("User Name");
            Map(m => m.UserEmail).Index(1).Name("User Email");
            Map(m => m.LastLoginDate).Index(2).Name("Last Login Date");
            Map(m => m.IsClientAdmin).Index(3).Name("Client Admin").ConvertUsing(m => m.IsClientAdmin ? "Yes" : "No");
            Map(m => m.IsContentPublisher).Index(4).Name("Content Publisher").ConvertUsing(m => m.IsContentPublisher ? "Yes" : "No");
            Map(m => m.IsContentAccessAdmin).Index(5).Name("Client Access Admin").ConvertUsing(m => m.IsContentAccessAdmin ? "Yes" : "No");
            Map(m => m.IsContentUser).Index(6).Name("Contennt User").ConvertUsing(m => m.IsContentUser ? "Yes" : "No");
            Map(m => m.IsFileDropAdmin).Index(7).Name("File Drop Admin").ConvertUsing(m => m.IsFileDropAdmin ? "Yes" : "No");
            Map(m => m.IsFileDropUser).Index(3).Name("File Drop User").ConvertUsing(m => m.IsFileDropUser ? "Yes" : "No");
        }
    }
}
