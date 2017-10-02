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
    }
}
