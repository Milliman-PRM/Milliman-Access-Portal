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
    public class ContentAccessAdminClientListViewModel
    {
        public List<ClientAndChildrenModel> ClientTreeList { get; set; } = new List<ClientAndChildrenModel>();
        public long RelevantClientId { get; set; } = 0;

        async public static Task<ContentAccessAdminClientListViewModel> Build(ApplicationUser CurrentUser, UserManager<ApplicationUser> UserManager, ApplicationDbContext DbContext)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion

            ContentAccessAdminClientListViewModel Model = new ContentAccessAdminClientListViewModel();

            // load into memory to improve speed and prevent lingering transactions
            List<Client> RootClients = DbContext.Client
                .Where(c => c.ParentClientId == null)
                .OrderBy(c => c.Name)
                .ToList();
            foreach (Client RootClient in RootClients)
            {
                ClientAndChildrenModel ClientModel = new ClientAndChildrenModel(RootClient);
                await ClientModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    Model.ClientTreeList.Add(ClientModel);
                }
            }

            return Model;
        }

    }

    public class ClientAndChildrenModel
    {
        public ContentAccessAdminClientDetailViewModel ClientDetailModel { get; set; }
        public List<ClientAndChildrenModel> ChildClientModels { get; set; } = new List<ClientAndChildrenModel>();

        public ClientAndChildrenModel()
        {
        }

        public ClientAndChildrenModel(Client ClientArg)
        {
            ClientDetailModel = new ContentAccessAdminClientDetailViewModel { ClientEntity = ClientArg };
        }

        async public Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser)
        {
            if (ClientDetailModel == null)
            {
                throw new MapException("ClientModel is uninitialized.");
            }

            await ClientDetailModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser);

            List<Client> ChildClients = DbContext.Client.Where(c => c.ParentClientId == ClientDetailModel.ClientEntity.Id).OrderBy(c => c.Name).ToList();
            foreach (Client ChildClient in ChildClients)
            {
                ClientAndChildrenModel ChildModel = new ClientAndChildrenModel(ChildClient);
                await ChildModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser);
                ChildClientModels.Add(ChildModel);
            }
        }

        public bool IsThisOrAnyChildManageable()
        {
            if (ClientDetailModel == null)
            {
                throw new MapException("ClientModel is uninitialized.");
            }

            if (ClientDetailModel.CanManage)
            {
                return true;
            }

            foreach (ClientAndChildrenModel Child in ChildClientModels)
            {
                if (Child.IsThisOrAnyChildManageable())
                {
                    return true;
                }
            }

            return false;
        }

    }
}
