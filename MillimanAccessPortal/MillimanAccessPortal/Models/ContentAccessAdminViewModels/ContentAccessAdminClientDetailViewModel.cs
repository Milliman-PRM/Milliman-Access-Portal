using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminClientDetailViewModel
    {
        public List<RootContentDetailViewModel> ContentList= new List<RootContentDetailViewModel>();

        internal static ContentAccessAdminClientDetailViewModel GetModel(long ClientId, ApplicationDbContext DbContext)
        {
            ContentAccessAdminClientDetailViewModel Model = new ContentAccessAdminClientDetailViewModel();

            foreach (var RootContent in DbContext.RootContentItem
                                                 .Include(rc => rc.ContentType)
                                                 .Where(rc => rc.ClientIdList.Contains(ClientId)))
            {
                Model.ContentList.Add(RootContentDetailViewModel.GetModel(RootContent, DbContext));
            }

            return Model;
        }
    }
}
