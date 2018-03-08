/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: A ViewModel for MAP
 * DEVELOPER NOTES:
 */

using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminRootContentItemListViewModel
    {
        public List<ContentAccessAdminRootContentItemDetailViewModel> RootContentItemList = new List<ContentAccessAdminRootContentItemDetailViewModel>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static ContentAccessAdminRootContentItemListViewModel Build(ApplicationDbContext DbContext, ApplicationUser CurrentUser, Client Client)
        {
            ContentAccessAdminRootContentItemListViewModel Model = new ContentAccessAdminRootContentItemListViewModel();

            List<RootContentItem> RootContentItems = DbContext.RootContentItem
                .Where(rci => rci.ClientId == Client.Id)
                .ToList();

            foreach (var RootContentItem in RootContentItems)
            {
                Model.RootContentItemList.Add(
                    ContentAccessAdminRootContentItemDetailViewModel.Build(DbContext, RootContentItem)
                    );
            }

            return Model;
        }
    }
}
