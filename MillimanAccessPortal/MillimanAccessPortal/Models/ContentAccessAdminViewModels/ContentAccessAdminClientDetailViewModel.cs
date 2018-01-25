/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapCommonLib;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminClientDetailViewModel
    {
        public Client ClientEntity { get; set; }
        public bool CanManage { get; set; }

        internal void GenerateSupportingProperties(ApplicationDbContext DbContext, ApplicationUser CurrentUser)
        {
            #region Validation
            if (ClientEntity == null)
            {
                throw new MapException("ContentAccessAdminClientDetailViewModel.GenerateSupportingProperties called with no ClientEntity set");
            }
            #endregion

            ClientEntity.ParentClient = null;

            CanManage = DbContext.UserRoleInClient
                .Include(urc => urc.Role)
                .Include(urc => urc.Client)
                .Where(urc => urc.UserId == CurrentUser.Id)
                .Where(urc => urc.Role.RoleEnum == RoleEnum.ContentAdmin)
                .Where(urc => urc.ClientId == ClientEntity.Id)
                .Any();
        }
    }
}
