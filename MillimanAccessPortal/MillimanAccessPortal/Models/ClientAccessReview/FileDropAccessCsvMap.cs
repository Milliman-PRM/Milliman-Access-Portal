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
    public class FileDropAccessRowItem
    {
        public string FileDropName { get; set; }
        public string UserGroupName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public bool CanDownload { get; set; }
        public bool CanUpload { get; set; }
        public bool CanDelete { get; set; }
    }

    public class FileDropAccessCsvMap : ClassMap<FileDropAccessRowItem>
    {
        public FileDropAccessCsvMap()
        {
            Map(m => m.FileDropName).Index(0).Name("File Drop Name");
            Map(m => m.UserGroupName).Index(1).Name("User Group");
            Map(m => m.UserName).Index(2).Name("User Name");
            Map(m => m.UserEmail).Index(3).Name("Email");
            Map(m => m.CanDownload).Index(4).Name("Download").ConvertUsing(m => m.CanDownload ? "Yes" : "No");
            Map(m => m.CanUpload).Index(5).Name("Upload").ConvertUsing(m => m.CanUpload ? "Yes" : "No");
            Map(m => m.CanDelete).Index(6).Name("Delete").ConvertUsing(m => m.CanDelete ? "Yes" : "No");
        }
    }
}
