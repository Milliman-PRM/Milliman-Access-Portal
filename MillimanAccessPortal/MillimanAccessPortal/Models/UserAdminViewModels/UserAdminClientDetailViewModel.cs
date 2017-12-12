using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.UserAdminViewModels
{
    public class UserAdminClientDetailViewModel
    {
        List<RootContentDetailViewModel> ContentList;

        internal static UserAdminClientDetailViewModel GetModel(long ClientId, ApplicationDbContext DbContext)
        {
            UserAdminClientDetailViewModel Model = new UserAdminClientDetailViewModel();

            foreach (var RootContent in DbContext.RootContentItem
                                                 .Include(rc => rc.ContentType)
                                                 .Where(rc => rc.ClientIdList.Contains(ClientId)))
            {
                Model.ContentList.Add(RootContentDetailViewModel.GetModel(RootContent));
            }

            return Model;
        }
    }
}
