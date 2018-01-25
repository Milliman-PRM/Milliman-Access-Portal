/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class IndexViewModel
    {
        public List<ClientAndChildrenModel> ClientTreeList { get; set; } = new List<ClientAndChildrenModel>();
        public long RelevantClientId { get; set; } = -1;

        public static IndexViewModel Build(ApplicationUser CurrentUser, ApplicationDbContext DbContext)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion

            IndexViewModel Model = new IndexViewModel();

            // load into memory to improve speed and prevent lingering transactions
            List<Client> RootClients = DbContext.Client
                .Where(c => c.ParentClientId == null)
                .OrderBy(c => c.Name)
                .ToList();
            foreach (Client RootClient in RootClients)
            {
                ClientAndChildrenModel ClientModel = new ClientAndChildrenModel(RootClient);
                ClientModel.GenerateSupportingProperties(DbContext, CurrentUser);
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

        public void GenerateSupportingProperties(ApplicationDbContext DbContext, ApplicationUser CurrentUser)
        {
            if (ClientDetailModel == null)
            {
                throw new MapException("ClientModel is uninitialized.");
            }

            ClientDetailModel.GenerateSupportingProperties(DbContext, CurrentUser);

            List<Client> ChildClients = DbContext.Client.Where(c => c.ParentClientId == ClientDetailModel.ClientEntity.Id).OrderBy(c => c.Name).ToList();
            foreach (Client ChildClient in ChildClients)
            {
                ClientAndChildrenModel ChildModel = new ClientAndChildrenModel(ChildClient);
                ChildModel.GenerateSupportingProperties(DbContext, CurrentUser);
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
