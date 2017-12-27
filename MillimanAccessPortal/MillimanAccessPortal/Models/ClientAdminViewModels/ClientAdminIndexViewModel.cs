/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing Clients and authorizations associated with actions that the current user is authorized to
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientAdminIndexViewModel
    {
        public List<ClientAndChildrenModel> ClientTreeList { get; set; } = new List<ClientAndChildrenModel>();
        public List<AuthorizedProfitCenterModel> AuthorizedProfitCenterList { get; set; } = new List<AuthorizedProfitCenterModel>();
        public long RelevantClientId { get; set; } = -1;
    }

    public class AuthorizedProfitCenterModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public AuthorizedProfitCenterModel(ProfitCenter Arg)
        {
            this.Id = Arg.Id;
            this.Name = Arg.Name;
            this.Code = Arg.ProfitCenterCode;
        }
    }

    public class ClientAndChildrenModel
    {
        public ClientDetailViewModel ClientModel { get; set; }
        public List<ClientAndChildrenModel> Children { get; set; } = new List<ClientAndChildrenModel>();

        public ClientAndChildrenModel(){}

        public ClientAndChildrenModel(Client ClientArg)
        {
            ClientModel = new ClientDetailViewModel { ClientEntity = ClientArg };
        }

        public async Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser, RoleEnum ClientRoleRequiredToManage, bool RequireProfitCenterAuthority)
        {
            if (ClientModel == null)
            {
                throw new MapException("Attempt to use instance of ClientAndChildrenModel before initialization");
            }

            await ClientModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser, ClientRoleRequiredToManage, RequireProfitCenterAuthority);

            List<Client> ChildrenOfThisClient = DbContext.Client.Where(c => c.ParentClientId == ClientModel.ClientEntity.Id).ToList();
            foreach (Client ChildOfThisClient in ChildrenOfThisClient)
            {
                ClientAndChildrenModel NextChild = new ClientAndChildrenModel(ChildOfThisClient);
                await NextChild.GenerateSupportingProperties(DbContext, UserManager, CurrentUser, ClientRoleRequiredToManage, RequireProfitCenterAuthority);
                Children.Add(NextChild);
            }

        }

        /// <summary>
        /// Convenience method to establish whether this instance or any child has CanManage == true
        /// </summary>
        /// <returns></returns>
        public bool IsThisOrAnyChildManageable()
        {
            if (ClientModel == null)
            {
                throw new MapException("Attempt to use instance of ClientAndChildrenModel before initialization");
            }

            if (ClientModel.CanManage) return true;
            foreach (ClientAndChildrenModel Child in Children)
            {
                if (Child.IsThisOrAnyChildManageable()) return true;
            }
            return false;
        }

    }
}
