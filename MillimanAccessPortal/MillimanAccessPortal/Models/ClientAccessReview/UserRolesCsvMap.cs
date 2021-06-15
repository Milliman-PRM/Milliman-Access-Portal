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

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class UserRolesCsvMap : ClassMap<ClientActorReviewModel>
    {
        private bool isClientAdmin;
        private bool isContentPublisher;
        private bool isContentAccessAdmin;
        private bool isContentUser;
        private bool isFileDropAdmin;
        private bool isFileDropUser;

        public UserRolesCsvMap()
        {
            Map(m => m.Name).Index(0).Name("User Name");
            Map(m => m.UserEmail).Index(1).Name("User Email");
            Map(m => m.LastLoginDate).Index(2).Name("Last Login Date");
            Map(m => m.ClientUserRoles).Index(3).Name("Client Admin")
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.Admin, out isClientAdmin) ? "Yes" : "No");
            Map(m => m.ClientUserRoles).Index(4)
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.ContentPublisher, out isContentPublisher) ? "Yes" : "No");
            Map(m => m.ClientUserRoles).Index(5).Name("Content Access Admin")
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.ContentAccessAdmin, out isContentAccessAdmin) ? "Yes" : "No");
            Map(m => m.ClientUserRoles).Index(6).Name("Content User")
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.ContentUser, out isContentUser) ? "Yes" : "No");
            Map(m => m.ClientUserRoles).Index(7).Name("File Drop Admin")
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.FileDropAdmin, out isFileDropAdmin) ? "Yes" : "No");
            Map(m => m.ClientUserRoles).Index(8).Name("File Drop User")
                .ConvertUsing(m => m.ClientUserRoles.TryGetValue(RoleEnum.FileDropUser, out isFileDropUser) ? "Yes" : "No");
            Map(m => m.IsSuspended).Index(9).Name("Suspended").ConvertUsing(m => m.IsSuspended ? "Yes" : "No");
            Map(m => m.IsAccountDisabled).Index(10).Name("Disabled").ConvertUsing(m => m.IsAccountDisabled ? "Yes" : "No");
        }
    }
}
