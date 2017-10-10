using System.Collections.Generic;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class ClientAndChildrenViewModel
    {
        public Client ClientEntity { get; set; }
        public bool CanManage { get; set; }
        public int AssociatedUserCount { get; set; } = 0;
        public int AssociatedContentCount { get; set; } = 0;
        public List<ClientAndChildrenViewModel> Children { get; set; } = new List<ClientAndChildrenViewModel>();

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
