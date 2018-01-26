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
    public class ContentAccessAdminRootContentItemListViewModel
    {
        public List<ContentAccessAdminRootContentItemDetailViewModel> RootContentItemList;
        public long RelevantRootContentItemId { get; set; } = -1;
    }
}
