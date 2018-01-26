/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminReportGroupListViewModel
    {
        public List<ContentAccessAdminReportGroupDetailViewModel> ReportGroupList = new List<ContentAccessAdminReportGroupDetailViewModel>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static ContentAccessAdminReportGroupListViewModel Build(ApplicationDbContext DbContext, Client Client, RootContentItem RootContentItem)
        {
            ContentAccessAdminReportGroupListViewModel Model = new ContentAccessAdminReportGroupListViewModel();

            return Model;
        }
    }
}
