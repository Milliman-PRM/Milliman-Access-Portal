using System.Collections.Generic;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientAdminIndexViewModel
    {
        public List<ClientAndChildrenModel> ClientTree = new List<ClientAndChildrenModel>();
        public List<AuthorizedProfitCenterModel> AuthorizedProfitCenterList = new List<AuthorizedProfitCenterModel>();
    }

    public class AuthorizedProfitCenterModel
    {
        public long Id;
        public string Name;
        public string Code;

        public AuthorizedProfitCenterModel(ProfitCenter Arg)
        {
            this.Id = Arg.Id;
            this.Name = Arg.Name;
            this.Code = Arg.ProfitCenterCode;
        }
    }

    public class ClientAndChildrenModel
    {
        public Client ClientEntity { get; set; }
        public bool CanManage { get; set; }
        public int AssociatedUserCount { get; set; } = 0;
        public int AssociatedContentCount { get; set; } = 0;
        public List<ClientAndChildrenModel> Children { get; set; } = new List<ClientAndChildrenModel>();

        /// <summary>
        /// Convenience method to establish whether this instance or any child has CanManage == true
        /// </summary>
        /// <returns></returns>
        public bool IsThisOrAnyChildManageable()
        {
            if (CanManage) return true;
            foreach (var Child in Children)
            {
                if (Child.IsThisOrAnyChildManageable()) return true;
            }
            return false;
        }

    }
}
