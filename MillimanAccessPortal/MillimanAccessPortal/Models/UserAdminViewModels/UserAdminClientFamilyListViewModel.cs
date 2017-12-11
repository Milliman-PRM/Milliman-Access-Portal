using System.Collections.Generic;
using MillimanAccessPortal.Models.ClientAdminViewModels;

namespace MillimanAccessPortal.Models.UserAdminViewModels
{
    public class UserAdminClientFamilyListViewModel
    {
        public List<ClientAndChildrenModel> ClientTree = new List<ClientAndChildrenModel>();
    }
}
