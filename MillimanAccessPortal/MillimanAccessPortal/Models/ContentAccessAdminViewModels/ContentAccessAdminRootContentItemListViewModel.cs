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
        public List<ContentAccessAdminRootContentItemDetailViewModel> RootContentItemList = new List<ContentAccessAdminRootContentItemDetailViewModel>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static ContentAccessAdminRootContentItemListViewModel Build(ApplicationDbContext DbContext, Client Client)
        {
            ContentAccessAdminRootContentItemListViewModel Model = new ContentAccessAdminRootContentItemListViewModel();

            List<RootContentItem> RootContentItems = DbContext.RootContentItem
                .Where(rci => rci.ClientIdList.Contains(Client.Id))
                .ToList();

            foreach (var RootContentItem in RootContentItems)
            {
                Model.RootContentItemList.Add(
                    ContentAccessAdminRootContentItemDetailViewModel.Build(DbContext, RootContentItem)
                    );
            }

            return Model;
        }

        internal static ContentAccessAdminRootContentItemListViewModel Build(ApplicationDbContext DbContext, long ClientId)
        {
            Client Client = DbContext.Client
                .Single(c => c.Id == ClientId);

            return Build(DbContext, Client);
        }
    }
}
