/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing details of a RootContentItem for use in ContentAccessAdmin
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminReportGroupDetailViewModel
    {
        internal static ContentAccessAdminReportGroupDetailViewModel Build(ApplicationDbContext DbContext, ContentItemUserGroup ReportGroup)
        {
            ContentAccessAdminReportGroupDetailViewModel Model = new ContentAccessAdminReportGroupDetailViewModel();

            return Model;
        }

        internal static ContentAccessAdminReportGroupDetailViewModel Build(long ReportGroupId, ApplicationDbContext DbContext)
        {
            ContentItemUserGroup ReportGroup = DbContext.ContentItemUserGroup
                .Single(rci => rci.Id == ReportGroupId);

            return Build(DbContext, ReportGroup);
        }
    }
}
