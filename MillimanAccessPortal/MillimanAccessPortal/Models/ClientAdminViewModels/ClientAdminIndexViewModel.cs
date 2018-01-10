/*
1 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing Clients and authorizations associated with actions that the current user is authorized to
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.DataQueries;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientAdminIndexViewModel
    {
        public List<ClientAndChildrenModel> ClientTreeList { get; set; } = new List<ClientAndChildrenModel>();
        public List<AuthorizedProfitCenterModel> AuthorizedProfitCenterList { get; set; } = new List<AuthorizedProfitCenterModel>();
        public long RelevantClientId { get; set; } = -1;

        public static async Task<ClientAdminIndexViewModel> GetClientAdminIndexModelForUser(ApplicationUser CurrentUser, UserManager<ApplicationUser> UserManager, ApplicationDbContext DbContext)
        {
            #region Validation
            if (CurrentUser == null)
            {
                return null;
            }
            #endregion

            StandardQueries Queries = new StandardQueries(DbContext, UserManager);

            // Instantiate working variables
            ClientAdminIndexViewModel ModelToReturn = new ClientAdminIndexViewModel();

            // Add all appropriate client trees
            List<Client> AllRootClients = Queries.GetAllRootClients();  // list to memory so utilization is fast and no lingering transaction
            foreach (Client RootClient in AllRootClients.OrderBy(c => c.Name))
            {
                //await Queries.GetDescendentFamilyOfClient(RootClient, CurrentUser, RoleEnum.Admin, true, true);
                ClientAndChildrenModel ClientModel = new ClientAndChildrenModel(RootClient);
                await ClientModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser, RoleEnum.Admin, true, true);
                if (ClientModel.IsThisOrAnyChildManageable())
                {
                    ModelToReturn.ClientTreeList.Add(ClientModel);
                }
            }

            // Add all ProfitCenterManager authorizations for the current user
            foreach (var AuthorizedProfitCenter in DbContext.UserRoleInProfitCenter
                                                            .Include(urpc => urpc.Role)
                                                            .Include(urpc => urpc.ProfitCenter)
                                                            .Where(urpc => urpc.Role.RoleEnum == RoleEnum.Admin
                                                                        && urpc.UserId == CurrentUser.Id)
                                                            .Distinct()
                                                            .Select(urpc => urpc.ProfitCenter))
            {
                ModelToReturn.AuthorizedProfitCenterList.Add(new AuthorizedProfitCenterModel(AuthorizedProfitCenter));
            }

            return ModelToReturn;
        }

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

    /// <summary>
    /// A model representing a Client and its associated details, along with a collection of child entities of this same type
    /// </summary>
    public class ClientAndChildrenModel
    {
        public ClientDetailViewModel ClientModel { get; set; }
        public List<ClientAndChildrenModel> Children { get; set; } = new List<ClientAndChildrenModel>();

        public ClientAndChildrenModel(){}

        public ClientAndChildrenModel(Client ClientArg)
        {
            ClientModel = new ClientDetailViewModel { ClientEntity = ClientArg };
        }

        /// <summary>
        /// Fully populates the Model properties, adds all children, and calls recursively for children
        /// </summary>
        /// <param name="DbContext"></param>
        /// <param name="UserManager"></param>
        /// <param name="CurrentUser"></param>
        /// <param name="ClientRoleRequiredToManage"></param>
        /// <param name="RequireProfitCenterAuthority"></param>
        /// <returns></returns>
        public async Task GenerateSupportingProperties(ApplicationDbContext DbContext, UserManager<ApplicationUser> UserManager, ApplicationUser CurrentUser, RoleEnum ClientRoleRequiredToManage, bool RequireProfitCenterAuthority, bool Recursive=true)
        {
            if (ClientModel == null)
            {
                throw new MapException("Attempt to use instance of ClientAndChildrenModel before initialization");
            }

            await ClientModel.GenerateSupportingProperties(DbContext, UserManager, CurrentUser, ClientRoleRequiredToManage, RequireProfitCenterAuthority);

            if (Recursive)
            {
                List<Client> ChildrenOfThisClient = DbContext.Client.Where(c => c.ParentClientId == ClientModel.ClientEntity.Id).OrderBy(c => c.Name).ToList();
                foreach (Client ChildOfThisClient in ChildrenOfThisClient)
                {
                    ClientAndChildrenModel NextChild = new ClientAndChildrenModel(ChildOfThisClient);
                    await NextChild.GenerateSupportingProperties(DbContext, UserManager, CurrentUser, ClientRoleRequiredToManage, RequireProfitCenterAuthority);
                    Children.Add(NextChild);
                }
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
